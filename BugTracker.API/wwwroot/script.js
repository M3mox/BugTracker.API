document.addEventListener("DOMContentLoaded", function () {
    const createForm = document.getElementById("create-bug-form");
    const assignedUserSelect = document.getElementById("assigned-user");
    const assignedUserGroup = document.getElementById("assigned-user-group"); // z.B. ein <div> oder <label> drum herum
    let currentlyEditingId = null;

    const token = localStorage.getItem("token");
    let userRole = null;
    let username = null;

    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            userRole = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
            username = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"];

            if (userRole === "admin") {
                document.body.classList.add("admin-mode");
                document.getElementById("admin-badge").classList.remove("hidden");
                assignedUserGroup.classList.remove("hidden"); // Nur Admins sehen die Auswahl
            } else {
                assignedUserGroup.classList.add("hidden");
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

    createForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        const title = document.getElementById("bug-title").value;
        const description = document.getElementById("bug-description").value;
        const status = document.getElementById("bug-status").value;

        // Nur Admins dürfen assignedTo setzen
        let assignedTo = null;
        if (userRole === "admin") {
            assignedTo = assignedUserSelect.value || null;
        }

        const bugData = {
            id: currentlyEditingId ?? 0,
            title,
            description,
            status
        };

        if (assignedTo) {
            bugData.assignedTo = assignedTo;
        }

        // Nur wenn neu und nicht admin, dann selbst als Ersteller setzen
        if (!currentlyEditingId && userRole !== "admin") {
            bugData.createdBy = username;
        }


        const url = currentlyEditingId
            ? `https://localhost:7063/api/Bugs/${currentlyEditingId}`
            : "https://localhost:7063/api/Bugs";

        const method = currentlyEditingId ? "PUT" : "POST";

        const response = await fetch(url, {
            method,
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify(bugData)
        });

        if (response.ok) {
            const isUpdate = currentlyEditingId !== null;
            createForm.reset();
            currentlyEditingId = null;
            document.getElementById("cancel-edit-btn").classList.add("hidden");
            document.getElementById("edit-hint").style.display = "none";
            document.getElementById("create-bug-form").classList.remove("edit-mode");
            document.querySelector("#create-bug-form button[type='submit']").textContent = "Create Ticket";
            await fetchBugs();

            Swal.fire({
                title: isUpdate ? "Ticket updated!" : "Ticket created!",
                icon: "success",
                timer: 1100,
                showConfirmButton: false
            });
        }
    });

    document.getElementById("cancel-edit-btn").addEventListener("click", () => {
        createForm.reset();
        currentlyEditingId = null;
        document.getElementById("cancel-edit-btn").classList.add("hidden");
        document.getElementById("edit-hint").style.display = "none";
        document.getElementById("create-bug-form").classList.remove("edit-mode");
        document.querySelector("#create-bug-form button[type='submit']").textContent = "Create Ticket";
    });

    async function fetchUsers() {
        if (userRole !== "admin") return;

        const res = await fetch("https://localhost:7063/api/Users", {
            headers: { "Authorization": `Bearer ${token}` }
        });
        const users = await res.json();

        assignedUserSelect.innerHTML = '<option value="">Assign to...</option>';
        users.forEach(user => {
            const opt = document.createElement("option");
            opt.value = user.username || user.email || user.name;
            opt.textContent = user.username || user.email || user.name;
            assignedUserSelect.appendChild(opt);
        });
    }

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

            const row = document.createElement("tr");
            row.classList.add("hover:bg-gray-50");

            let actionButtons = "";
            if (userRole === "admin" || bug.createdBy === username) {
                actionButtons += `<button onclick="editBug(event, ${bug.id})" class="bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm">Edit</button>`;
            }
            if (userRole === "admin") {
                actionButtons += `<button onclick="deleteBug(event, ${bug.id})" class="bg-red-600 hover:bg-red-700 text-white px-3 py-1 rounded text-sm">Delete</button>`;
            }

            row.addEventListener("click", () => showBugDetails(bug));
            row.innerHTML = `
                <td class="px-6 py-4 border-b text-sm text-gray-700">${bug.title}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${bug.status}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${createdAtFormatted}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${updatedAtFormatted}</td>
                <td class="px-6 py-4 border-b text-sm text-gray-700">${bug.assignedTo || "-"}</td>
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

    async function deleteBug(event, id) {
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
    }

    window.editBug = async function (event, id) {
        event.stopPropagation();

        const response = await fetch(`https://localhost:7063/api/Bugs/${id}`, {
            headers: { "Authorization": `Bearer ${token}` }
        });

        const bug = await response.json();

        if (userRole !== "admin" && bug.createdBy !== username) {
            return Swal.fire("Access denied", "You are not allowed to edit this ticket.", "error");
        }

        document.getElementById("bug-title").value = bug.title;
        document.getElementById("bug-description").value = bug.description;
        document.getElementById("bug-status").value = bug.status;

        if (userRole === "admin") {
            assignedUserSelect.value = bug.assignedTo || "";
        }

        currentlyEditingId = bug.id;

        document.querySelector("#create-bug-form button[type='submit']").textContent = "Update";
        document.getElementById("cancel-edit-btn").classList.remove("hidden");
        document.getElementById("edit-hint").innerText = "🛠️ Edit-mode is activated.";
        document.getElementById("edit-hint").style.display = "block";
        document.getElementById("create-bug-form").classList.add("edit-mode");
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

    fetchUsers();
    fetchBugs();
});
