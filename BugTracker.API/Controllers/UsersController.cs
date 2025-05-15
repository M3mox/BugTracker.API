using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        // Hardcoded Users
        private readonly List<object> _users = new()
        {
            new { Username = "admin" },
            new { Username = "employee" }
        };

        // Autorisierung auf "admin"-Rolle prüfen
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult GetUsers()
        {
            return Ok(_users);
        }
    }
}
