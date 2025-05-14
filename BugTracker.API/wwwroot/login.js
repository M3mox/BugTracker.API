document.getElementById("login-form").addEventListener("submit", async function (e) {
    e.preventDefault();

    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value;

    const response = await fetch("https://localhost:7063/api/auth/login", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({ username, password })
    });

    const errorBox = document.getElementById("login-error");

    if (response.ok) {
        const data = await response.json();
        const token = data.token;

        localStorage.setItem("token", token);

        const payload = JSON.parse(atob(token.split('.')[1]));
        const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        const name = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"];

        localStorage.setItem("loggedInUser", JSON.stringify({ username: name, role }));

        window.location.href = "index.html";
    } else {
        errorBox.textContent = "Login failed: Username or password incorrect.";
        errorBox.classList.remove("hidden");
        Swal.fire({
            title: "Login failed",
            text: "Username or password incorrect.",
            icon: "error",
            timer: 1100,
            showConfirmButton: false
        });
    }
});