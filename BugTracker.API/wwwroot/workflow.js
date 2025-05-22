document.addEventListener("DOMContentLoaded", function () {
    const token = localStorage.getItem("token");
    let userRole = null;
    let username = null;

    // Authentication check
    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            userRole = payload["role"];
            username = payload["unique_name"];

            if (userRole === "admin") {
                document.body.classList.add("admin-mode");
                document.getElementById("admin-badge").classList.remove("hidden");
                document.getElementById("workflow-stats").classList.remove("hidden");
            }
        } catch (err) {
            console.error("Token decoding failed:", err);
            redirectToLogin("Invalid token. Please log in again.");
        }
    } else {
        redirectToLogin("Not logged in. Please try again.");
    }

    // Initialize workflow page
    loadWorkflowDiagram();
    loadStatusOverview();
    if (userRole === "admin") {
        loadWorkflowStatistics();
    }

    async function loadWorkflowDiagram() {
        try {
            const response = await fetch("https://localhost:7063/api/Workflow/diagram", {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const workflow = await response.json();
            displayWorkflowDiagram(workflow);
        } catch (error) {
            console.error("Error loading workflow diagram:", error);
        }
    }

    async function loadStatusOverview() {
        try {
            const response = await fetch("https://localhost:7063/api/Workflow/statuses", {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const statuses = await response.json();
            displayStatusOverview(statuses);
        } catch (error) {
            console.error("Error loading status overview:", error);
        }
    }

    async function loadWorkflowStatistics() {
        try {
            const response = await fetch("https://localhost:7063/api/Workflow/statistics", {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const stats = await response.json();
            displayWorkflowStatistics(stats);
        } catch (error) {
            console.error("Error loading workflow statistics:", error);
        }
    }

    function displayWorkflowDiagram(workflow) {
        const diagramContainer = document.querySelector("#workflow-diagram .grid");
        diagramContainer.innerHTML = "";

        workflow.nodes.forEach(node => {
            const nodeDiv = document.createElement("div");
            nodeDiv.className = "bg-white p-4 rounded border-2 text-center shadow-sm";
            nodeDiv.style.borderColor = node.color;

            nodeDiv.innerHTML = `
                <div class="w-4 h-4 rounded-full mx-auto mb-2" style="background-color: ${node.color}"></div>
                <h3 class="font-semibold text-sm">${node.label}</h3>
                <p class="text-xs text-gray-600 mt-1">${node.description}</p>
            `;

            diagramContainer.appendChild(nodeDiv);
        });
    }

    function displayStatusOverview(statuses) {
        const overviewContainer = document.getElementById("status-overview");
        overviewContainer.innerHTML = "";

        statuses.forEach(status => {
            const statusCard = document.createElement("div");
            statusCard.className = "bg-white p-4 rounded border border-gray-200 shadow-sm";

            statusCard.innerHTML = `
                <h3 class="font-semibold text-lg">${status.displayName}</h3>
                <p class="text-sm text-gray-600 mt-2">${status.description}</p>
                <div class="mt-3">
                    <button class="text-blue-600 hover:text-blue-800 text-sm" 
                            onclick="showTransitions('${status.value}')">
                        View Transitions →
                    </button>
                </div>
            `;

            overviewContainer.appendChild(statusCard);
        });
    }

    function displayWorkflowStatistics(stats) {
        document.getElementById("total-bugs").textContent = stats.totalBugs || "0";
        document.getElementById("completed-month").textContent = stats.statusDistribution?.Completed || "0";
        document.getElementById("in-progress").textContent = stats.statusDistribution?.["In Progress"] || "0";
    }

    // Global function for showing transitions
    window.showTransitions = async function (fromStatus) {
        try {
            const response = await fetch(`https://localhost:7063/api/Workflow/transitions/${fromStatus}`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const transitions = await response.json();

            const transitionsText = transitions.length > 0
                ? transitions.join(", ")
                : "No transitions available";

            Swal.fire({
                title: `Transitions from ${fromStatus}`,
                html: `<p class="text-gray-700">Available transitions:</p><p class="font-semibold text-blue-600">${transitionsText}</p>`,
                icon: "info",
                confirmButtonText: "OK"
            });
        } catch (error) {
            console.error("Error loading transitions:", error);
            Swal.fire({
                title: "Error",
                text: "Failed to load transitions",
                icon: "error"
            });
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
});