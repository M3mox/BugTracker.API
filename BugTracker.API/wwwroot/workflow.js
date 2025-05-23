document.addEventListener("DOMContentLoaded", function () {
    const token = localStorage.getItem("token");
    let userRole = null;
    let username = null;

    // Status display names mapping
    const statusDisplayNames = {
        "Open": "Open",
        "InProgress": "In Progress",
        "Testing": "Testing",
        "Completed": "Completed",
        "Rejected": "Rejected",
        "OnHold": "On Hold",
        "Failed": "Failed",
        "Reopened": "Reopened"
    };

    // Status colors for visual representation
    const statusColors = {
        "Open": "bg-blue-100 text-blue-800 border-blue-200",
        "InProgress": "bg-yellow-100 text-yellow-800 border-yellow-200",
        "Testing": "bg-purple-100 text-purple-800 border-purple-200",
        "Completed": "bg-green-100 text-green-800 border-green-200",
        "Rejected": "bg-red-100 text-red-800 border-red-200",
        "OnHold": "bg-gray-100 text-gray-800 border-gray-200",
        "Failed": "bg-orange-100 text-orange-800 border-orange-200",
        "Reopened": "bg-indigo-100 text-indigo-800 border-indigo-200"
    };

    // Workflow transitions definition
    const workflowTransitions = {
        "Open": ["InProgress", "Rejected"],
        "InProgress": ["Testing", "OnHold", "Open"],
        "Testing": ["Completed", "Failed"],
        "Completed": ["Reopened"],
        "Rejected": ["Reopened"],
        "OnHold": ["InProgress", "Rejected"],
        "Failed": ["InProgress"],
        "Reopened": ["InProgress", "Rejected"]
    };

    // Check authentication and get user role
    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            userRole = payload["role"];
            username = payload["unique_name"];

            console.log("User info:", { userRole, username }); // Debug

            if (userRole === "admin") {
                document.body.classList.add("admin-mode");
                document.getElementById("admin-badge").classList.remove("hidden");
                document.getElementById("workflow-stats").classList.remove("hidden");
                loadWorkflowStatistics();
            }
        } catch (err) {
            console.error("Token decoding failed:", err);
            redirectToLogin("Invalid token. Please log in again.");
            return;
        }
    } else {
        redirectToLogin("Not logged in. Please try again.");
        return;
    }

    // Initialize the workflow page
    initializeWorkflowDiagram();
    loadStatusOverview();

    function initializeWorkflowDiagram() {
        const diagramContainer = document.getElementById("workflow-diagram");
        const gridContainer = diagramContainer.querySelector(".grid");

        // Clear existing content
        gridContainer.innerHTML = "";

        // Create workflow diagram
        Object.keys(workflowTransitions).forEach(status => {
            const statusCard = document.createElement("div");
            statusCard.className = `p-4 rounded border-2 ${statusColors[status]} transition-all hover:shadow-md`;

            const displayName = statusDisplayNames[status] || status;
            const transitions = workflowTransitions[status];

            statusCard.innerHTML = `
                <div class="font-semibold text-lg mb-2">${displayName}</div>
                <div class="text-sm mb-3">
                    <strong>Can transition to:</strong>
                </div>
                <div class="space-y-1">
                    ${transitions.map(targetStatus => {
                const targetDisplayName = statusDisplayNames[targetStatus] || targetStatus;
                return `<div class="text-xs px-2 py-1 bg-white bg-opacity-50 rounded">
                            → ${targetDisplayName}
                        </div>`;
            }).join('')}
                </div>
            `;

            gridContainer.appendChild(statusCard);
        });
    }

    async function loadStatusOverview() {
        try {
            const response = await fetch("https://localhost:7063/api/Bugs", {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const bugs = await response.json();
            console.log("Bugs loaded for status overview:", bugs); // Debug

            displayStatusOverview(bugs);
        } catch (error) {
            console.error("Error loading bugs for status overview:", error);
            Swal.fire({
                title: "Error",
                text: "Failed to load status overview. Please try again.",
                icon: "error"
            });
        }
    }

    function displayStatusOverview(bugs) {
        const overviewContainer = document.getElementById("status-overview");
        overviewContainer.innerHTML = "";

        // Count bugs by status
        const statusCounts = {};
        Object.keys(statusDisplayNames).forEach(status => {
            statusCounts[status] = 0;
        });

        bugs.forEach(bug => {
            const status = bug.status;
            if (statusCounts.hasOwnProperty(status)) {
                statusCounts[status]++;
            } else {
                console.warn(`Unknown status found: ${status}`);
                statusCounts[status] = (statusCounts[status] || 0) + 1;
            }
        });

        console.log("Status counts:", statusCounts); // Debug

        // Create status cards
        Object.entries(statusCounts).forEach(([status, count]) => {
            const displayName = statusDisplayNames[status] || status;
            const colorClass = statusColors[status] || "bg-gray-100 text-gray-800 border-gray-200";

            const statusCard = document.createElement("div");
            statusCard.className = `p-4 rounded border-2 ${colorClass} cursor-pointer transition-all hover:shadow-md`;
            statusCard.addEventListener("click", () => filterBugsByStatus(status));

            statusCard.innerHTML = `
                <div class="font-semibold text-lg">${displayName}</div>
                <div class="text-2xl font-bold mt-2">${count}</div>
                <div class="text-sm mt-1 opacity-75">
                    ${count === 1 ? 'bug' : 'bugs'}
                </div>
            `;

            overviewContainer.appendChild(statusCard);
        });
    }

    async function loadWorkflowStatistics() {
        try {
            const response = await fetch("https://localhost:7063/api/Bugs", {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const bugs = await response.json();
            console.log("Bugs loaded for statistics:", bugs); // Debug

            displayWorkflowStatistics(bugs);
        } catch (error) {
            console.error("Error loading workflow statistics:", error);
            document.getElementById("total-bugs").textContent = "Error";
            document.getElementById("completed-month").textContent = "Error";
            document.getElementById("in-progress").textContent = "Error";
        }
    }

    function displayWorkflowStatistics(bugs) {
        const totalBugs = bugs.length;

        // Count completed bugs this month
        const currentMonth = new Date().getMonth();
        const currentYear = new Date().getFullYear();
        const completedThisMonth = bugs.filter(bug => {
            if (bug.status !== "Completed") return false;

            const updatedDate = new Date(bug.updatedAt || bug.createdAt);
            return updatedDate.getMonth() === currentMonth &&
                updatedDate.getFullYear() === currentYear;
        }).length;

        // Count in-progress bugs
        const inProgressBugs = bugs.filter(bug => bug.status === "InProgress").length;

        // Update UI
        document.getElementById("total-bugs").textContent = totalBugs;
        document.getElementById("completed-month").textContent = completedThisMonth;
        document.getElementById("in-progress").textContent = inProgressBugs;

        console.log("Statistics:", { totalBugs, completedThisMonth, inProgressBugs }); // Debug
    }

    function filterBugsByStatus(status) {
        // Navigate to dashboard with status filter
        const displayName = statusDisplayNames[status] || status;
        Swal.fire({
            title: "Filter by Status",
            text: `Show all bugs with status: ${displayName}`,
            icon: "info",
            showCancelButton: true,
            confirmButtonText: "Go to Dashboard",
            cancelButtonText: "Cancel"
        }).then((result) => {
            if (result.isConfirmed) {
                // You can implement status filtering on the dashboard
                // For now, just redirect to dashboard
                window.location.href = `index.html?status=${encodeURIComponent(status)}`;
            }
        });
    }

    // Role-based permission checker
    function canUserTransition(fromStatus, toStatus) {
        // This is a simplified version - in a real app, this would check against the backend
        const rolePermissions = {
            "admin": true, // Admin can do everything
            "user": {
                // Users can generally transition their own bugs
                "Open": ["InProgress"],
                "InProgress": ["Testing", "OnHold", "Open"],
                "Testing": [], // Only admin can complete testing
                "OnHold": ["InProgress"],
                "Failed": ["InProgress"],
                "Reopened": ["InProgress"]
            }
        };

        if (userRole === "admin") return true;

        const userTransitions = rolePermissions.user[fromStatus] || [];
        return userTransitions.includes(toStatus);
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

    // Add some interactive features
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            window.location.href = "index.html";
        }
    });

    // Add refresh functionality
    const refreshButton = document.createElement("button");
    refreshButton.className = "fixed bottom-6 right-6 bg-blue-600 hover:bg-blue-700 text-white p-3 rounded-full shadow-lg transition-all";
    refreshButton.innerHTML = "🔄";
    refreshButton.title = "Refresh Data";
    refreshButton.addEventListener("click", () => {
        loadStatusOverview();
        if (userRole === "admin") {
            loadWorkflowStatistics();
        }

        Swal.fire({
            title: "Refreshed!",
            text: "Workflow data has been updated.",
            icon: "success",
            timer: 1000,
            showConfirmButton: false
        });
    });

    document.body.appendChild(refreshButton);
});