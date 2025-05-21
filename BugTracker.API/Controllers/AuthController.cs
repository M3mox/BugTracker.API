using BugTracker.API.Service;
using BugTracker.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace BugTracker.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;

        public AuthController(IConfiguration configuration, UserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            var user = _userService.GetUser(login.Username, login.Password);
            if (user == null)
                return Unauthorized("Invalid credentials");

            // Claims für JWT
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // JWT Key vorbereiten + Länge überprüfen
            var keyBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            if (keyBytes.Length < 32)
            {
                var extended = new byte[32];
                Array.Copy(keyBytes, extended, keyBytes.Length);
                keyBytes = extended;
            }
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = creds,
                Subject = new ClaimsIdentity(claims)
            };

            var securityHandler = new JwtSecurityTokenHandler();
            var token = securityHandler.CreateToken(tokenDescriptor);
            return Ok(new
            {
                token = securityHandler.WriteToken(token),
                username = user.Username,
                role = user.Role
            });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel registerModel)
        {
            // Prüfen, ob der Benutzername bereits existiert
            var existingUser = _userService.GetUsers().FirstOrDefault(u =>
                u.Username.Equals(registerModel.Username, StringComparison.OrdinalIgnoreCase));

            if (existingUser != null)
                return Conflict("Username already exists.");

            // Benutzer mit gehashtem Passwort erstellen
            var newUser = _userService.CreateUser(
                registerModel.Username,
                registerModel.Password,
                "user" // Standardrolle
            );

            return Ok(new
            {
                username = newUser.Username,
                role = newUser.Role,
                message = "User registered successfully"
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword([FromBody] ChangePasswordModel model)
        {
            // Aktuelle User-ID aus dem Token holen
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Altes Passwort verifizieren
            var user = _userService.GetUser(User.Identity.Name, model.CurrentPassword);
            if (user == null)
                return BadRequest("Current password is incorrect");

            // Passwort ändern
            bool success = _userService.UpdatePassword(userId, model.NewPassword);
            if (!success)
                return BadRequest("Failed to update password");

            return Ok(new { message = "Password changed successfully" });
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}