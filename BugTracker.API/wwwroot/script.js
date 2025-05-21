document.addEventListener("DOMContentLoaded", function () {
    const token = localStorage.getItem("token");
    let userRole = null;
    let username = null;

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

    document.getElementById("logout-button").addEventListener("click", () => {
        localStorage.removeItem("token");
        localStorage.removeItem("loggedInUser");
        window.location.href = "login.html";
    });

    document.getElementById("close-modal-btn").addEventListener("click", function () {
        document.getElementById("bug-detail-modal").classList.add("hidden");
    });

    async function fetchBugs() {
        const response = await fetch("https://localhost:7063/api/Bugs", {
            headers: { "Authorization": `Bearer ${token}` }
        });

        const bugs = await response.json();
        const bugList = document.getElementById("bug-list");
        bugList.innerHTML = "";

        bugs.forEach(bug => {
            const createdAtFormatted = new Date(bug.createdAt).toLocaleString("de-DE");
            const updatedAtFormatted = bug.updatedAt ? new Date(bug.updatedAt).toLocaleString("de-DE") : "-";
            const createdByUsername = bug.createdBy?.username || "-";

            const row = document.createElement("tr");
            row.classList.add("hover:bg-gray-50");

            // Check if user can edit this bug (user is creator OR admin)
            const canEdit = userRole === "admin" || createdByUsername === username;
            // Only admin can delete bugs
            const canDelete = userRole === "admin";

            let actionButtons = "";
            if (canEdit) {
                actionButtons += `<a href="create-ticket.html?id=${bug.id}" class="bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm mr-1">Edit</a>`;
            }
            if (canDelete) {
                actionButtons += `<button onclick="deleteBug(event, ${bug.id})" class="bg-red-600 hover:bg-red-700 text-white px-3 py-1 rounded text-sm">Delete</button>`;
            }

            row.addEventListener("click", () => showBugDetails(bug));
            row.innerHTML = `
                <td class="px-6 py-4 border-b text-sm text-gray-700">${bug.title}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${bug.status}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${createdAtFormatted}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${updatedAtFormatted}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${bug.assignedTo?.username || "-"}</td>
                <td class="px-6 py-4 border-b whitespace-nowrap text-sm text-right space-x-2">${actionButtons}</td>
            `;

            bugList.appendChild(row);
        });
    }

    function showBugDetails(bug) {
        document.getElementById("bug-detail-title").innerText = bug.title;
        document.getElementById("bug-detail-description").innerText = bug.description;
        document.getElementById("bug-detail-modal").classList.remove("hidden");
    }

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
            const response = await fetch(`https://localhost:7063/api/Bugs/${id}`, {
                method: 'DELETE',
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (response.ok) {
                await fetchBugs();
                Swal.fire("Deleted!", "The Ticket was deleted successfully.", "success");
            } else {
                Swal.fire("Error", "The Ticket could not be deleted.", "error");
            }
        }
    };

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

    fetchBugs();
});