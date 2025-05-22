document.addEventListener("DOMContentLoaded", function () {
    const createForm = document.getElementById("create-bug-form");
    const assignedUserSelect = document.getElementById("assigned-user");

    let currentlyEditingId = null;

    const token = localStorage.getItem("token");
    let userRole = null;
    let username = null;

    // Check if we have an ID in the URL (editing mode)
    const urlParams = new URLSearchParams(window.location.search);
    const ticketId = urlParams.get('id');

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

    document.getElementById("cancel-edit-btn").addEventListener("click", () => {
        window.location.href = "index.html";
    });

    createForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        const title = document.getElementById("bug-title").value;
        const description = document.getElementById("bug-description").value;
        const status = document.getElementById("bug-status").value;

        // all users are allowed to set the assigned to
        let assignedTo = null;
        assignedTo = assignedUserSelect.value || null;

        const bugData = {
            id: currentlyEditingId ?? 0,
            title,
            description,
            status
        };

        if (assignedTo) {
            bugData.assignedToID = assignedTo;
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

            Swal.fire({
                title: isUpdate ? "Ticket updated!" : "Ticket created!",
                icon: "success",
                timer: 1100,
                showConfirmButton: false
            }).then(() => {
                // Redirect back to dashboard
                window.location.href = "index.html";
            });
        } else if (response.status === 403) {
            Swal.fire({
                title: "Access denied",
                text: "You don't have permission to edit this ticket.",
                icon: "error",
                timer: 2000,
                showConfirmButton: false
            });
        }
    });

    async function fetchUsers() {
        if (!assignedUserSelect) return; // Security-Check instead of role-banning

        const res = await fetch("https://localhost:7063/api/Users", {
            headers: { "Authorization": `Bearer ${token}` }
        });

        if (!res.ok) {
            const errorText = await res.text();
            console.error(`Error fetching users: ${res.status}`, errorText);
            return;
        }

        const users = await res.json();

        assignedUserSelect.innerHTML = '<option value="">Assign to...</option>';

        const usernames = users.map(user => user.username || user.name);

        if (!usernames.includes(username)) {
            const selfOption = document.createElement("option");
            selfOption.value = username;
            selfOption.textContent = `${username} (You)`;
            assignedUserSelect.appendChild(selfOption);
        }

        users.forEach(user => {
            const displayName = user.username || user.name;
            const opt = document.createElement("option");
            opt.value = user.id;
            opt.textContent = displayName === username ? `${displayName} (You)` : displayName;
            assignedUserSelect.appendChild(opt);
        });

        console.log("Fetched users for assignment:", users);
    }

    async function loadBugForEditing(id) {
        const response = await fetch(`https://localhost:7063/api/Bugs/${id}`, {
            headers: { "Authorization": `Bearer ${token}` }
        });

        if (!response.ok) {
            Swal.fire({
                title: "Error",
                text: "Could not load the ticket data. Redirecting to dashboard.",
                icon: "error",
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                window.location.href = "index.html";
            });
            return;
        }

        const bug = await response.json();

        // Debugging zum Verständnis des API-Responses
        console.log("Bug data from API:", bug);

        document.getElementById("bug-title").value = bug.title;
        document.getElementById("bug-description").value = bug.description;
        document.getElementById("bug-status").value = bug.status;

        if (assignedUserSelect) {
            // check, if a user is assigned and in which format the data is
            if (bug.assignedToID) {
                // Case 1: API directly gives assignedToId back
                assignedUserSelect.value = bug.assignedToID;
            } else if (bug.assignedTo && typeof bug.assignedTo === 'object' && bug.assignedTo.id) {
                // Case 2: API gives back an assignedTo-Object with an ID
                assignedUserSelect.value = bug.assignedTo.id;
            } else if (bug.assignedTo && typeof bug.assignedTo === 'string') {
                // Case 3: API gives assignedTo back as a String (evtl. it is the ID)
                assignedUserSelect.value = bug.assignedTo;
            } else {
                // no user assigned or unknown format
                assignedUserSelect.value = "";
            }

            console.log("Setting assignedUserSelect value to:", assignedUserSelect.value);
        }

        currentlyEditingId = bug.id;

        document.querySelector("#create-bug-form button[type='submit']").textContent = "Update";
        document.getElementById("cancel-edit-btn").classList.remove("hidden");
        document.getElementById("edit-hint").innerText = "🛠️ Edit-mode is activated.";
        document.getElementById("edit-hint").style.display = "block";
        document.getElementById("create-bug-form").classList.add("edit-mode");
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

    // Load data
    fetchUsers();

    // If we have a ticket ID in the URL, load it for editing
    if (ticketId) {
        loadBugForEditing(ticketId);
    }
});