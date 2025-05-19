using BugTracker.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BugTracker.API.DTO;

namespace BugTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService userService = new UserService();

        // Autorisierung auf "admin"-Rolle prüfen
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult GetUsers()
        {
            var users = userService.GetUsers().Select(u => new UserDTO { Username = u.Username, Role = u.Role });
            return Ok(users);
        }
    }
}
