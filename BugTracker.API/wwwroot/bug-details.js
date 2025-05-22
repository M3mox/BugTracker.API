document.addEventListener("DOMContentLoaded", function () {
    const token = localStorage.getItem("token");
    let userRole = null;
    let username = null;
    let currentBugId = null;

    // Get bug ID from URL
    const urlParams = new URLSearchParams(window.location.search);
    const bugId = urlParams.get('id');

    if (!bugId) {
        redirectToDashboard("No bug ID specified");
        return;
    }

    // Check authentication and get user role
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

    // Initialize the page
    loadBugDetails(bugId);
    loadComments(bugId);
    loadWorkflowInfo(bugId);

    // Set up comment form submission
    const commentForm = document.getElementById("comment-form");
    commentForm.addEventListener("submit", function (e) {
        e.preventDefault();
        submitComment(bugId);
    });

    // Functions
    async function loadBugDetails(id) {
        try {
            const response = await fetch(`https://localhost:7063/api/Bugs/${id}`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const bug = await response.json();
            currentBugId = bug.id;

            // Update UI with bug details
            document.getElementById("bug-title").textContent = bug.title;
            document.getElementById("bug-status").textContent = bug.status;
            document.getElementById("bug-assigned").textContent = bug.assignedTo?.username || "Not assigned";
            document.getElementById("bug-created").textContent = new Date(bug.createdAt).toLocaleString("de-DE");
            document.getElementById("bug-updated").textContent = bug.updatedAt ?
                new Date(bug.updatedAt).toLocaleString("de-DE") : "Not updated";
            document.getElementById("bug-description").textContent = bug.description;

            // Setup action buttons
            setupActionButtons(bug);

        } catch (error) {
            console.error("Error loading bug details:", error);
            Swal.fire({
                title: "Error",
                text: "Failed to load bug details. Please try again.",
                icon: "error"
            }).then(() => {
                window.location.href = "index.html";
            });
        }
    }

    async function loadWorkflowInfo(bugId) {
        try {
            const response = await fetch(`https://localhost:7063/api/Bugs/${bugId}/workflow`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                console.log("Workflow info not available yet");
                return;
            }

            const workflowInfo = await response.json();
            displayWorkflowInfo(workflowInfo);
        } catch (error) {
            console.error("Error loading workflow info:", error);
        }
    }

    function displayWorkflowInfo(workflowInfo) {
        // Add workflow section after bug details
        const bugDetailsSection = document.getElementById("bug-details");

        // Remove existing workflow section if it exists
        const existingWorkflow = document.getElementById("workflow-section");
        if (existingWorkflow) {
            existingWorkflow.remove();
        }

        const workflowSection = document.createElement("div");
        workflowSection.id = "workflow-section";
        workflowSection.className = "mb-8 bg-gray-50 p-6 rounded border border-gray-200";

        let statusTransitionsHTML = "";
        if (workflowInfo.allowedTransitions && workflowInfo.allowedTransitions.length > 0) {
            statusTransitionsHTML = workflowInfo.allowedTransitions.map(transition =>
                `<button class="bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm mr-2 mb-2" 
                         onclick="transitionStatus('${transition}')">${transition}</button>`
            ).join("");
        } else {
            statusTransitionsHTML = '<p class="text-gray-500 italic">No status transitions available</p>';
        }

        let statusHistoryHTML = "";
        if (workflowInfo.statusHistory && workflowInfo.statusHistory.length > 0) {
            statusHistoryHTML = workflowInfo.statusHistory.map(history =>
                `<div class="flex justify-between items-center py-2 border-b border-gray-200 last:border-b-0">
                    <div>
                        <span class="text-sm font-medium">${history.fromStatus} → ${history.toStatus}</span>
                        ${history.comment ? `<p class="text-xs text-gray-600">${history.comment}</p>` : ''}
                    </div>
                    <div class="text-xs text-gray-500">
                        <div>${history.changedBy?.username || 'System'}</div>
                        <div>${new Date(history.transitionDate).toLocaleString("de-DE")}</div>
                    </div>
                </div>`
            ).join("");
        } else {
            statusHistoryHTML = '<p class="text-gray-500 italic">No status history available</p>';
        }

        workflowSection.innerHTML = `
            <h3 class="text-lg font-semibold mb-4">🔄 Workflow Management</h3>
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                    <h4 class="font-medium mb-2">Current Status</h4>
                    <p class="text-lg font-semibold text-blue-600 mb-4">${workflowInfo.currentStatus}</p>
                    
                    <h4 class="font-medium mb-2">Available Transitions</h4>
                    <div class="mb-4">
                        ${statusTransitionsHTML}
                    </div>
                </div>
                
                <div>
                    <h4 class="font-medium mb-2">Status History</h4>
                    <div class="bg-white p-4 rounded border border-gray-200 max-h-64 overflow-y-auto">
                        ${statusHistoryHTML}
                    </div>
                </div>
            </div>
        `;

        bugDetailsSection.after(workflowSection);
    }

    function setupActionButtons(bug) {
        const actionsContainer = document.getElementById("bug-actions");
        actionsContainer.innerHTML = "";

        // Check if user can edit this bug
        const canEdit = userRole === "admin" ||
            (bug.createdBy?.username === username) ||
            (bug.assignedTo?.username === username);

        // Only admin can delete bugs
        const canDelete = userRole === "admin";

        if (canEdit) {
            const editButton = document.createElement("a");
            editButton.href = `create-ticket.html?id=${bug.id}`;
            editButton.className = "bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm";
            editButton.textContent = "Edit";
            actionsContainer.appendChild(editButton);
        }

        if (canDelete) {
            const deleteButton = document.createElement("button");
            deleteButton.className = "bg-red-600 hover:bg-red-700 text-white px-3 py-1 rounded text-sm";
            deleteButton.textContent = "Delete";
            deleteButton.addEventListener("click", () => deleteBug(bug.id));
            actionsContainer.appendChild(deleteButton);
        }

        // Add workflow button
        const workflowButton = document.createElement("a");
        workflowButton.href = "workflow.html";
        workflowButton.className = "bg-purple-600 hover:bg-purple-700 text-white px-3 py-1 rounded text-sm";
        workflowButton.textContent = "Workflow";
        actionsContainer.appendChild(workflowButton);
    }

    // Global function for status transitions
    window.transitionStatus = async function (newStatus) {
        const { value: comment } = await Swal.fire({
            title: `Transition to ${newStatus}`,
            input: 'textarea',
            inputLabel: 'Comment (optional)',
            inputPlaceholder: 'Add a comment about this status change...',
            showCancelButton: true,
            confirmButtonText: 'Confirm Transition',
            cancelButtonText: 'Cancel'
        });

        if (comment !== undefined) { // User clicked confirm (even with empty comment)
            try {
                const response = await fetch(`https://localhost:7063/api/Bugs/${currentBugId}/transition`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": `Bearer ${token}`
                    },
                    body: JSON.stringify({
                        newStatus: newStatus,
                        comment: comment || null
                    })
                });

                if (!response.ok) {
                    const errorData = await response.text();
                    throw new Error(`HTTP error! Status: ${response.status} - ${errorData}`);
                }

                const result = await response.json();

                Swal.fire({
                    title: "Status Updated!",
                    text: `Status changed to: ${result.newStatus}`,
                    icon: "success",
                    timer: 1500,
                    showConfirmButton: false
                });

                // Reload bug details and workflow info
                await loadBugDetails(currentBugId);
                await loadWorkflowInfo(currentBugId);

            } catch (error) {
                console.error("Error transitioning status:", error);
                Swal.fire({
                    title: "Error",
                    text: "Failed to update status. You may not have permission for this transition.",
                    icon: "error"
                });
            }
        }
    };

    async function loadComments(bugId) {
        try {
            const response = await fetch(`https://localhost:7063/api/Bugs/${bugId}/comments`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                if (response.status === 404) {
                    const commentsList = document.getElementById("comments-list");
                    commentsList.innerHTML = '<p class="text-gray-500 italic">No comments yet.</p>';
                    return;
                }
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const comments = await response.json();
            displayComments(comments);
        } catch (error) {
            console.error("Error loading comments:", error);
            const commentsList = document.getElementById("comments-list");
            commentsList.innerHTML = '<p class="text-gray-500 italic">Unable to load comments.</p>';
        }
    }

    function displayComments(comments) {
        const commentsList = document.getElementById("comments-list");
        commentsList.innerHTML = "";

        if (!comments || comments.length === 0) {
            commentsList.innerHTML = '<p class="text-gray-500 italic">No comments yet.</p>';
            return;
        }

        comments.forEach(comment => {
            const commentDiv = document.createElement("div");
            commentDiv.className = "bg-white p-4 rounded border border-gray-200";

            const canDeleteComment = userRole === "admin" || comment.createdBy?.username === username;

            commentDiv.innerHTML = `
                <div class="flex justify-between items-start">
                    <div class="font-medium">${comment.createdBy?.username || "Anonymous"}</div>
                    <div class="text-sm text-gray-500">${new Date(comment.createdAt).toLocaleString("de-DE")}</div>
                </div>
                <div class="mt-2 whitespace-pre-wrap">${comment.text}</div>
                ${canDeleteComment ?
                    `<div class="mt-2 text-right">
                        <button class="text-red-600 hover:text-red-800 text-sm" data-comment-id="${comment.id}">Delete</button>
                    </div>` : ''}
            `;

            if (canDeleteComment) {
                const deleteBtn = commentDiv.querySelector(`button[data-comment-id="${comment.id}"]`);
                if (deleteBtn) {
                    deleteBtn.addEventListener("click", () => deleteComment(comment.id));
                }
            }

            commentsList.appendChild(commentDiv);
        });
    }

    async function submitComment(bugId) {
        const commentText = document.getElementById("comment-text").value.trim();

        if (!commentText) return;

        try {
            const response = await fetch(`https://localhost:7063/api/Bugs/${bugId}/comments`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({
                    text: commentText,
                    bugId: bugId
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            document.getElementById("comment-text").value = "";
            await loadComments(bugId);

            Swal.fire({
                title: "Comment Posted",
                text: "Your comment has been added successfully.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false
            });
        } catch (error) {
            console.error("Error submitting comment:", error);
            Swal.fire({
                title: "Error",
                text: "Failed to submit your comment.",
                icon: "error"
            });
        }
    }

    async function deleteComment(commentId) {
        const result = await Swal.fire({
            title: "Delete Comment?",
            text: "This action cannot be undone.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#3085d6",
            confirmButtonText: "Yes, delete it"
        });

        if (result.isConfirmed) {
            try {
                const response = await fetch(`https://localhost:7063/api/Comments/${commentId}`, {
                    method: "DELETE",
                    headers: { "Authorization": `Bearer ${token}` }
                });

                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }

                await loadComments(currentBugId);

                Swal.fire({
                    title: "Deleted!",
                    text: "Your comment has been deleted.",
                    icon: "success",
                    timer: 1500,
                    showConfirmButton: false
                });
            } catch (error) {
                console.error("Error deleting comment:", error);
                Swal.fire({
                    title: "Error",
                    text: "Failed to delete the comment.",
                    icon: "error"
                });
            }
        }
    }

    async function deleteBug(bugId) {
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
                const response = await fetch(`https://localhost:7063/api/Bugs/${bugId}`, {
                    method: 'DELETE',
                    headers: { "Authorization": `Bearer ${token}` }
                });

                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }

                Swal.fire({
                    title: "Deleted!",
                    text: "The Ticket was deleted successfully.",
                    icon: "success",
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => {
                    window.location.href = "index.html";
                });
            } catch (error) {
                console.error("Error deleting bug:", error);
                Swal.fire({
                    title: "Error",
                    text: "The Ticket could not be deleted.",
                    icon: "error"
                });
            }
        }
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

    function redirectToDashboard(message) {
        Swal.fire({
            title: "Error",
            text: message,
            icon: "warning",
            showConfirmButton: false,
            timer: 1800
        });

        setTimeout(() => {
            window.location.href = "index.html";
        }, 1800);
    }
});