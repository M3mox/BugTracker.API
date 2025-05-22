using BugTracker.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BugTracker.API.DTO;
using System.Security.Claims;

namespace BugTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            this._userService = userService;
        }

        // Autorisierung prüfen
        [HttpGet]
        [Authorize]
        public IActionResult GetUsers()
        {
            var users = _userService.GetUsers().Select(u => new UserDTO
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role
            });
            return Ok(users);
        }

        [HttpGet("userinfo")]
        public IActionResult GetUserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Die User ID zurückgeben
            return Ok(new { userId = userId });
        }
    }
}