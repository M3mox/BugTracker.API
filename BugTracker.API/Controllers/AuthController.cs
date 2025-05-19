using BugTracker.API.Service;
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
        private readonly UserService userService = new UserService();

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            var user = userService.GetUser(login.Username, login.Password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            // Claims für JWT
            var claims = new[]
            {
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

            //var token = new JwtSecurityToken(
            //    issuer: _configuration["Jwt:Issuer"],
            //    audience: _configuration["Jwt:Audience"],
            //    claims: claims,
            //    //expires: DateTime.UtcNow.AddHours(1),
            //    signingCredentials: creds
            //);

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
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

}