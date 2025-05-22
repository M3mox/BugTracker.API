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
            const actionsContainer = document.getElementById("bug-actions");
            actionsContainer.innerHTML = "";

            // Check if user can edit this bug
            // User can edit if: Admin OR Created the bug OR Assigned to the bug
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

    async function loadComments(bugId) {
        try {
            const response = await fetch(`https://localhost:7063/api/Bugs/${bugId}/comments`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                // If the endpoint doesn't exist yet, don't show an error
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
            // Display a more subtle error for comments since it might not be implemented yet
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

            // Clear the form and reload comments
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
                text: "Failed to submit your comment. The comment feature may not be implemented yet on the server.",
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

                // Reload comments
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