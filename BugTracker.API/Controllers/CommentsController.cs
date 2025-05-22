using Microsoft.AspNetCore.Mvc;
using BugTracker.Api.Data;
using BugTracker.Api.Models;
using BugTracker.API.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BugTracker.API.Service;
using System.Security.Claims;

namespace BugTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly BugContext _context;
        private readonly UserService _userService;

        public CommentsController(BugContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/Comments/5 - Get comment by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<CommentDTO>> GetComment(int id)
        {
            var comment = await _context.Comments
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
                return NotFound();

            var commentDTO = new CommentDTO
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                BugId = comment.BugId,
                CreatedBy = comment.CreatedBy != null ? new UserDTO
                {
                    Id = comment.CreatedBy.Id,
                    Username = comment.CreatedBy.Username,
                    Role = comment.CreatedBy.Role
                } : null
            };

            return commentDTO;
        }

        // POST: api/Comments - Create new comment
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CommentDTO>> CreateComment([FromBody] CreateCommentDTO commentDTO)
        {
            if (string.IsNullOrWhiteSpace(commentDTO.Text))
                return BadRequest("Comment text is required");

            // Check if bug exists
            var bug = await _context.Bugs.FindAsync(commentDTO.BugId);
            if (bug == null)
                return BadRequest("Bug not found");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userService.GetById(userId);

            var comment = new Comment
            {
                Text = commentDTO.Text,
                BugId = commentDTO.BugId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Return the created comment as DTO
            var createdCommentDTO = new CommentDTO
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                BugId = comment.BugId,
                CreatedBy = new UserDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    Role = user.Role
                }
            };

            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, createdCommentDTO);
        }

        // PUT: api/Comments/5 - Update comment
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] CreateCommentDTO commentDTO)
        {
            var comment = await _context.Comments
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("admin");

            // Check if user is authorized to update this comment
            if (!isAdmin && (comment.CreatedBy == null || comment.CreatedBy.Id != userId))
                return Forbid("You can only edit comments you created.");

            if (string.IsNullOrWhiteSpace(commentDTO.Text))
                return BadRequest("Comment text is required");

            comment.Text = commentDTO.Text;
            comment.UpdatedAt = DateTime.UtcNow;

            _context.Entry(comment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Comments/5 - Delete comment
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("admin");

            // Check if user is authorized to delete this comment
            if (!isAdmin && (comment.CreatedBy == null || comment.CreatedBy.Id != userId))
                return Forbid("You can only delete comments you created.");

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}