
document.addEventListener("DOMContentLoaded", function () {
    const registerForm = document.getElementById("register-form");
    const errorBox = document.getElementById("register-error");

    registerForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        const username = document.getElementById("username").value.trim();
        const password = document.getElementById("password").value;
        const confirmPassword = document.getElementById("confirm-password").value;

        // Validierung
        if (password !== confirmPassword) {
            errorBox.textContent = "Passwords do not match!";
            errorBox.classList.remove("hidden");
            return;
        }

        // Password complexity check
        if (password.length < 6) {
            errorBox.textContent = "Password must be at least 6 characters long!";
            errorBox.classList.remove("hidden");
            return;
        }

        try {
            const response = await fetch("https://localhost:7063/api/auth/register", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ username, password })
            });

            if (response.ok) {
                // Registrierung erfolgreich
                Swal.fire({
                    title: "Success!",
                    text: "Registration successful! You can now log in.",
                    icon: "success",
                    timer: 2000,
                    showConfirmButton: false
                });

                // Nach kurzer Verzögerung zur Login-Seite weiterleiten
                setTimeout(() => {
                    window.location.href = "login.html";
                }, 2000);
            } else {
                const errorData = await response.json();
                let errorMessage = "Registration failed";

                if (response.status === 409) {
                    errorMessage = "Username already exists. Please choose another one.";
                } else if (errorData.message) {
                    errorMessage = errorData.message;
                }

                errorBox.textContent = errorMessage;
                errorBox.classList.remove("hidden");
            }
        } catch (error) {
            console.error("Registration error:", error);
            errorBox.textContent = "An error occurred during registration. Please try again.";
            errorBox.classList.remove("hidden");
        }
    });
});