document.addEventListener("DOMContentLoaded", function () {
    const token = localStorage.getItem("token");
    let userRole = null;
    let username = null;
    let allBugs = [];
    let statusList = [];

    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            userRole = payload["role"];
            username = payload["unique_name"];

            if (userRole === "admin") {
                document.body.classList.add("admin-mode");
                document.getElementById("admin-badge").classList.remove("hidden");
            }
        } catch (err) {
            console.error("Token decoding failed:", err);
            redirectToLogin("Invalid token. Please log in again.");
        }
    } else {
        redirectToLogin("Not logged in. Please try again.");
    }

    // Event Listeners
    document.getElementById("logout-button").addEventListener("click", logout);
    document.getElementById("status-filter").addEventListener("change", applyFilters);
    document.getElementById("assigned-filter").addEventListener("change", applyFilters);
    document.getElementById("search-input").addEventListener("input", debounce(applyFilters, 300));
    document.getElementById("clear-filters").addEventListener("click", clearFilters);

    // Initialize
    loadStatusOverview();
    loadBugData();

    async function loadStatusOverview() {
        try {
            const response = await fetch("https://localhost:7063/api/Workflow/statuses", {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                console.log("Status overview not available");
                return;
            }

            const statuses = await response.json();
            statusList = statuses;
            displayStatusOverview(statuses);
            populateStatusFilter(statuses);
        } catch (error) {
            console.error("Error loading status overview:", error);
        }
    }

    function displayStatusOverview(statuses) {
        const overviewContainer = document.getElementById("status-overview");
        overviewContainer.innerHTML = "";

        statuses.forEach(status => {
            const count = allBugs.filter(bug => bug.status === status.displayName).length;

            const statusCard = document.createElement("div");
            statusCard.className = "bg-white p-4 rounded border border-gray-200 shadow-sm cursor-pointer hover:shadow-md transition-shadow";
            statusCard.onclick = () => filterByStatus(status.displayName);

            statusCard.innerHTML = `
                <h3 class="font-semibold text-lg text-gray-800">${status.displayName}</h3>
                <p class="text-2xl font-bold text-blue-600">${count}</p>
                <p class="text-sm text-gray-600 mt-1">${status.description}</p>
            `;

            overviewContainer.appendChild(statusCard);
        });
    }

    function populateStatusFilter(statuses) {
        const statusFilter = document.getElementById("status-filter");
        statusFilter.innerHTML = '<option value="">All Status</option>';

        statuses.forEach(status => {
            const option = document.createElement("option");
            option.value = status.displayName;
            option.textContent = status.displayName;
            statusFilter.appendChild(option);
        });
    }

    async function loadBugData() {
        showLoading(true);
        try {
            const response = await fetch("https://localhost:7063/api/Bugs", {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            allBugs = await response.json();
            displayBugs(allBugs);
            displayStatusOverview(statusList); // Update counts
        } catch (error) {
            console.error("Error loading bugs:", error);
            Swal.fire({
                title: "Error",
                text: "Failed to load bug data.",
                icon: "error"
            });
        } finally {
            showLoading(false);
        }
    }

    function displayBugs(bugs) {
        const bugList = document.getElementById("bug-list");
        bugList.innerHTML = "";

        if (bugs.length === 0) {
            bugList.innerHTML = `
                <tr>
                    <td colspan="7" class="px-6 py-8 text-center text-gray-500">
                        No bugs found matching your filters.
                    </td>
                </tr>
            `;
            return;
        }

        bugs.forEach(bug => {
            createBugRow(bug).then(row => {
                bugList.appendChild(row);
            });
        });
    }

    async function createBugRow(bug) {
        const createdAtFormatted = new Date(bug.createdAt).toLocaleString("de-DE");
        const updatedAtFormatted = bug.updatedAt ? new Date(bug.updatedAt).toLocaleString("de-DE") : "-";
        const createdByUsername = bug.createdBy?.username || "-";
        const assignedToUsername = bug.assignedTo?.username || "-";

        const row = document.createElement("tr");
        row.classList.add("hover:bg-gray-50");

        // Permission checks
        const canEdit = userRole === "admin" || createdByUsername === username || assignedToUsername === username;
        const canDelete = userRole === "admin";

        // Get allowed transitions for this bug
        let quickActions = `<div class="flex flex-wrap gap-1">`;

        try {
            // Map the bug status to the correct enum value for API call
            const statusMapping = {
                'Open': 'Open',
                'In Progress': 'InProgress',
                'Testing': 'Testing',
                'Completed': 'Completed',
                'Rejected': 'Rejected',
                'On Hold': 'OnHold',
                'Failed': 'Failed',
                'Reopened': 'Reopened'
            };

            const apiStatus = statusMapping[bug.status] || bug.status;
            const transitionsResponse = await fetch(`https://localhost:7063/api/Workflow/transitions/${apiStatus}`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (transitionsResponse.ok) {
                const allowedTransitions = await transitionsResponse.json();

                // Show max 3 quick transitions to avoid crowding
                allowedTransitions.slice(0, 3).forEach(transition => {
                    const buttonColor = getTransitionButtonColor(transition);
                    quickActions += `<button onclick="quickTransition(event, ${bug.id}, '${transition}')" 
                                           class="${buttonColor} text-white px-2 py-1 rounded text-xs hover:opacity-80">
                                           ${getTransitionLabel(bug.status, transition)}
                                    </button>`;
                });

                if (allowedTransitions.length > 3) {
                    quickActions += `<button onclick="showAllTransitions(event, ${bug.id}, '${bug.status}')"
                                           class="bg-gray-500 text-white px-2 py-1 rounded text-xs hover:bg-gray-600">
                                           +${allowedTransitions.length - 3}
                                    </button>`;
                }
            }
        } catch (error) {
            console.error("Error loading transitions:", error);
            quickActions += '<span class="text-xs text-gray-500">-</span>';
        }

        quickActions += `</div>`;

        // Regular action buttons
        let actionButtons = "";
        if (canEdit) {
            actionButtons += `<a href="create-ticket.html?id=${bug.id}" 
                                class="bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm mr-1">
                                Edit
                              </a>`;
        }
        if (canDelete) {
            actionButtons += `<button onclick="deleteBug(event, ${bug.id})" 
                                    class="bg-red-600 hover:bg-red-700 text-white px-3 py-1 rounded text-sm">
                                    Delete
                              </button>`;
        }

        // Status color coding
        const statusClass = getStatusColorClass(bug.status);

        row.addEventListener("click", (e) => {
            if (e.target.tagName === 'BUTTON' || e.target.tagName === 'A') {
                return;
            }
            window.location.href = `bug-details.html?id=${bug.id}`;
        });

        row.innerHTML = `
            <td class="px-6 py-4 border-b text-sm text-gray-700">${bug.title}</td>
            <td class="px-6 py-4 border-b text-sm">
                <span class="px-2 py-1 rounded-full text-xs font-medium ${statusClass}">
                    ${bug.status}
                </span>
            </td>
            <td class="px-6 py-4 border-b text-sm">${quickActions}</td>
            <td class="px-6 py-4 border-b text-sm text-gray-700">${createdAtFormatted}</td>
            <td class="px-6 py-4 border-b text-sm text-gray-700">${updatedAtFormatted}</td>
            <td class="px-6 py-4 border-b text-sm text-gray-700">${assignedToUsername}</td>
            <td class="px-6 py-4 border-b whitespace-nowrap text-sm text-right space-x-2">${actionButtons}</td>
        `;

        return row;
    }

    function getStatusColorClass(status) {
        const statusColors = {
            'Open': 'bg-blue-100 text-blue-800',
            'In Progress': 'bg-yellow-100 text-yellow-800',
            'Testing': 'bg-purple-100 text-purple-800',
            'Completed': 'bg-green-100 text-green-800',
            'Rejected': 'bg-red-100 text-red-800',
            'On Hold': 'bg-gray-100 text-gray-800',
            'Failed': 'bg-red-200 text-red-900',
            'Reopened': 'bg-orange-100 text-orange-800'
        };
        return statusColors[status] || 'bg-gray-100 text-gray-800';
    }

    function getTransitionButtonColor(transition) {
        const transitionColors = {
            'In Progress': 'bg-yellow-500 hover:bg-yellow-600',
            'Testing': 'bg-purple-500 hover:bg-purple-600',
            'Completed': 'bg-green-500 hover:bg-green-600',
            'Rejected': 'bg-red-500 hover:bg-red-600',
            'On Hold': 'bg-gray-500 hover:bg-gray-600',
            'Failed': 'bg-red-600 hover:bg-red-700',
            'Reopened': 'bg-orange-500 hover:bg-orange-600',
            'Open': 'bg-blue-500 hover:bg-blue-600'
        };
        return transitionColors[transition] || 'bg-gray-500 hover:bg-gray-600';
    }

    function getTransitionLabel(currentStatus, targetStatus) {
        const labels = {
            'In Progress': 'Start',
            'Testing': 'Test',
            'Completed': 'Complete',
            'Rejected': 'Reject',
            'On Hold': 'Hold',
            'Failed': 'Fail',
            'Reopened': 'Reopen',
            'Open': 'Open'
        };
        return labels[targetStatus] || targetStatus;
    }

    // Filter functions
    function applyFilters() {
        const statusFilter = document.getElementById("status-filter").value;
        const assignedFilter = document.getElementById("assigned-filter").value;
        const searchTerm = document.getElementById("search-input").value.toLowerCase();

        let filteredBugs = allBugs.filter(bug => {
            // Status filter
            if (statusFilter && bug.status !== statusFilter) return false;

            // Assignment filter
            if (assignedFilter === "unassigned" && bug.assignedTo?.username) return false;
            if (assignedFilter === "me" && bug.assignedTo?.username !== username) return false;

            // Search filter
            if (searchTerm && !bug.title.toLowerCase().includes(searchTerm) &&
                !bug.description?.toLowerCase().includes(searchTerm)) return false;

            return true;
        });

        displayBugs(filteredBugs);
    }

    function filterByStatus(status) {
        document.getElementById("status-filter").value = status;
        applyFilters();
    }

    function clearFilters() {
        document.getElementById("status-filter").value = "";
        document.getElementById("assigned-filter").value = "";
        document.getElementById("search-input").value = "";
        displayBugs(allBugs);
    }

    // Show all transitions modal
    window.showAllTransitions = async function (event, bugId, currentStatus) {
        event.stopPropagation();

        try {
            const statusMapping = {
                'Open': 'Open',
                'In Progress': 'InProgress',
                'Testing': 'Testing',
                'Completed': 'Completed',
                'Rejected': 'Rejected',
                'On Hold': 'OnHold',
                'Failed': 'Failed',
                'Reopened': 'Reopened'
            };

            const apiStatus = statusMapping[currentStatus] || currentStatus;
            const response = await fetch(`https://localhost:7063/api/Workflow/transitions/${apiStatus}`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const transitions = await response.json();

            let buttonsHtml = '';
            transitions.forEach(transition => {
                const buttonColor = getTransitionButtonColor(transition);
                buttonsHtml += `<button onclick="quickTransition(null, ${bugId}, '${transition}'); Swal.close();" 
                                      class="${buttonColor} text-white px-4 py-2 rounded m-1 hover:opacity-80">
                                      ${transition}
                                </button>`;
            });

            Swal.fire({
                title: `All Transitions from ${currentStatus}`,
                html: `<div class="text-center">${buttonsHtml}</div>`,
                showConfirmButton: false,
                showCancelButton: true,
                cancelButtonText: 'Close'
            });

        } catch (error) {
            console.error("Error loading all transitions:", error);
            Swal.fire({
                title: "Error",
                text: "Failed to load transitions",
                icon: "error"
            });
        }
    };

    // Quick status transition
    window.quickTransition = async function (event, bugId, newStatus) {
        if (event) event.stopPropagation();

        try {
            const response = await fetch(`https://localhost:7063/api/Bugs/${bugId}/transition`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({
                    newStatus: newStatus,
                    comment: `Quick transition to ${newStatus} from dashboard`
                })
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`HTTP error! Status: ${response.status} - ${errorText}`);
            }

            // Reload data to reflect changes
            await loadBugData();

            Swal.fire({
                title: "Status Updated!",
                text: `Status changed to: ${newStatus}`,
                icon: "success",
                timer: 1500,
                showConfirmButton: false
            });

        } catch (error) {
            console.error("Error transitioning status:", error);
            Swal.fire({
                title: "Error",
                text: "Failed to update status. You may not have permission for this transition.",
                icon: "error"
            });
        }
    };

    // Delete function
    window.deleteBug = async function (event, id) {
        event.stopPropagation();

        const result = await Swal.fire({
            title: "Are you sure?",
            text: "This Ticket will be deleted permanently!",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#3085d6",
            confirmButtonText: "Yes, I confirm!",
            cancelButtonText: "Cancel"
        });

        if (result.isConfirmed) {
            try {
                const response = await fetch(`https://localhost:7063/api/Bugs/${id}`, {
                    method: 'DELETE',
                    headers: { "Authorization": `Bearer ${token}` }
                });

                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }

                await loadBugData();
                Swal.fire("Deleted!", "The Ticket was deleted successfully.", "success");
            } catch (error) {
                console.error("Error deleting bug:", error);
                Swal.fire("Error", "The Ticket could not be deleted.", "error");
            }
        }
    };

    function logout() {
        localStorage.removeItem("token");
        localStorage.removeItem("loggedInUser");
        window.location.href = "login.html";
    }

    function redirectToLogin(message) {
        Swal.fire({
            title: "Access denied",
            text: message,
            icon: "warning",
            showConfirmButton: false,
            timer: 1800
        });

        setTimeout(() => {
            window.location.href = "login.html";
        }, 1800);
    }

    function showLoading(show) {
        const indicator = document.getElementById("loading-indicator");
        const table = document.querySelector(".overflow-auto");

        if (show) {
            indicator.classList.remove("hidden");
            table.classList.add("opacity-50");
        } else {
            indicator.classList.add("hidden");
            table.classList.remove("opacity-50");
        }
    }

    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
});