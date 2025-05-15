using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BugTracker.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            // Beispiel-User
            var users = new List<UserModel>
            {
                new UserModel { Username = "admin", Password = "admin123", Role = "admin" },
                new UserModel { Username = "employee", Password = "user123", Role = "employee" }
            };

            var user = users.FirstOrDefault(u =>
                u.Username == login.Username && u.Password == login.Password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            // Claims für JWT
            var claims = new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", user.Username),
                new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", user.Role)
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

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                //expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                username = user.Username,
                role = user.Role
            });
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // Beispiel-Nutzer
    public class UserModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}