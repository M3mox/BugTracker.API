<script>
    const createForm = document.getElementById("create-bug-form");
    let currentlyEditingId = null;

    // Abrufen der Bugs von der API
    async function fetchBugs() {
            const response = await fetch("https://localhost:7063/api/Bugs");
    const bugs = await response.json();

    const bugList = document.getElementById("bug-list");
    bugList.innerHTML = "";

            bugs.forEach(bug => {
                const createdAtFormatted = new Date(bug.createdAt).toLocaleString("de-DE");
    const updatedAtFormatted = bug.updatedAt
    ? new Date(bug.updatedAt).toLocaleString("de-DE")
    : "-";

    const row = document.createElement("tr");
    row.innerHTML = `
    <td>${bug.title}</td>
    <td>${bug.status}</td>
    <td>${createdAtFormatted}</td>
    <td>${updatedAtFormatted}</td>
    <td>
        <button class="btn btn-primary" onclick="editBug(${bug.id})">Edit</button>
        <button class="btn btn-danger" onclick="deleteBug(${bug.id})">Delete</button>
    </td>
    `;
    bugList.appendChild(row);
            });
        }


    // Bug löschen
    async function deleteBug(id) {
            const response = await fetch(`https://localhost:7063/api/Bugs/${id}`, {method: 'DELETE' });
    if (response.ok) {
        fetchBugs();
            }
        }

    // Bug bearbeiten
    function editBug(id) {
        fetch(`https://localhost:7063/api/Bugs/${id}`)
            .then(response => response.json())
            .then(bug => {
                document.getElementById("bug-title").value = bug.title;
                document.getElementById("bug-description").value = bug.description;
                document.getElementById("bug-status").value = bug.status;
                currentlyEditingId = bug.id;

                document.querySelector("#create-bug-form button[type='submit']").textContent = "Bug aktualisieren";
                document.getElementById("cancel-edit-btn").style.display = "inline-block";
                document.getElementById("create-bug-form").classList.add("edit-mode");
                document.getElementById("edit-hint").style.display = "block";
            });
        }

    // Bug erstellen oder aktualisieren
    createForm.addEventListener("submit", async function (e) {
        e.preventDefault();

    const title = document.getElementById("bug-title").value;
    const description = document.getElementById("bug-description").value;
    const status = document.getElementById("bug-status").value;

    const bugData = {
        id: currentlyEditingId ?? 0,
    title: title,
    description: description,
    status: status,
    createdAt: new Date().toISOString()
            };

    const url = currentlyEditingId
    ? `https://localhost:7063/api/Bugs/${currentlyEditingId}`
    : "https://localhost:7063/api/Bugs";

    const method = currentlyEditingId ? "PUT" : "POST";

    const response = await fetch(url, {
        method: method,
    headers: {"Content-Type": "application/json" },
    body: JSON.stringify(bugData)
            });

    if (response.ok) {
        createForm.reset();
    fetchBugs();
    currentlyEditingId = null;
    document.querySelector("#create-bug-form button[type='submit']").textContent = "Bug erstellen";
    document.getElementById("cancel-edit-btn").style.display = "none";
    document.getElementById("create-bug-form").classList.remove("edit-mode");
    document.getElementById("edit-hint").style.display = "none";
            } else {
        alert("Fehler beim Speichern des Bugs.");
            }
        });

        // Bearbeiten abbrechen
        document.getElementById("cancel-edit-btn").addEventListener("click", () => {
        createForm.reset();
    currentlyEditingId = null;
    document.querySelector("#create-bug-form button[type='submit']").textContent = "Bug erstellen";
    document.getElementById("cancel-edit-btn").style.display = "none";
    document.getElementById("create-bug-form").classList.remove("edit-mode");
    document.getElementById("edit-hint").style.display = "none";
        });

    // Beim Laden Bugs abrufen
    window.onload = fetchBugs;
</script>