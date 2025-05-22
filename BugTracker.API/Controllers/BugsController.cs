using Microsoft.AspNetCore.Mvc;
using BugTracker.Api.Data;
using BugTracker.Api.Models;
using BugTracker.API.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BugTracker.API.Service;
using System.Security.Claims;

namespace BugTracker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BugsController : ControllerBase
{
    private readonly BugContext _context;
    private readonly UserService _userService;

    public BugsController(BugContext context, UserService userService)
    {
        _context = context;
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bug>>> GetBugs()
    {
        var bugs = await _context.Bugs.Include(b => b.AssignedTo).Include(b => b.CreatedBy).ToListAsync();
        return bugs;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Bug>> GetBug(int id)
    {
        var bug = await _context.Bugs
            .Include(b => b.AssignedTo)
            .Include(b => b.CreatedBy)
            .FirstOrDefaultAsync(b => b.Id == id);
        return bug == null ? NotFound() : bug;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Bug>> CreateBug([FromBody] CreateBugDTO bugDTO)
    {
        Console.WriteLine($"Incoming bug: {bugDTO.Title}");
        if (string.IsNullOrWhiteSpace(bugDTO.Title))
            return BadRequest("Title is missing");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bug = new Bug
        {
            Title = bugDTO.Title,
            Description = bugDTO.Description,
            Status = bugDTO.Status,
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = _userService.GetById(userId),
            AssignedTo = string.IsNullOrEmpty(bugDTO.AssignedToID) ? null : _userService.GetById(bugDTO.AssignedToID)
        };

        _context.Bugs.Add(bug);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBug), new { id = bug.Id }, bug);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateBug(int id, CreateBugDTO bugDTO)
    {
        var bug = await _context.Bugs
            .Include(b => b.CreatedBy)
            .Include(b => b.AssignedTo)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bug == null)
            return NotFound();

        // Get current user info
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("admin");

        // Check if user is authorized to update this bug
        // User can edit if: Admin OR Created the bug OR Assigned to the bug
        bool canEdit = isAdmin ||
                      (bug.CreatedBy != null && bug.CreatedBy.Id == userId) ||
                      (bug.AssignedTo != null && bug.AssignedTo.Id == userId);

        if (!canEdit)
            return Forbid("You can only edit bugs you created or bugs assigned to you.");

        // Update fields from dto
        bug.Title = bugDTO.Title;
        bug.Description = bugDTO.Description;
        bug.Status = bugDTO.Status;
        bug.UpdatedAt = DateTime.Now;

        // Only update assignedTo if a value was provided
        if (!string.IsNullOrEmpty(bugDTO.AssignedToID))
        {
            bug.AssignedTo = _userService.GetById(bugDTO.AssignedToID);
        }

        // Save
        _context.Entry(bug).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteBug(int id)
    {
        var bug = await _context.Bugs.FindAsync(id);
        if (bug == null) return NotFound();

        _context.Bugs.Remove(bug);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // COMMENT ENDPOINTS FOR SPECIFIC BUG

    // GET: api/Bugs/5/comments - Get all comments for a specific bug
    [HttpGet("{id}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDTO>>> GetBugComments(int id)
    {
        try
        {
            Console.WriteLine($"Fetching comments for bug ID: {id}");

            // Check if bug exists
            var bugExists = await _context.Bugs.AnyAsync(b => b.Id == id);
            if (!bugExists)
            {
                Console.WriteLine($"Bug with ID {id} not found");
                return NotFound("Bug not found");
            }

            Console.WriteLine($"Bug {id} exists, checking if Comments table exists...");

            // Check if the Comments table exists
            var commentsTableExists = _context.Model.FindEntityType(typeof(Comment)) != null;

            if (!commentsTableExists)
            {
                Console.WriteLine("Comments table does not exist yet - returning empty list");
                return Ok(new List<CommentDTO>());
            }

            var comments = await _context.Comments
                .Include(c => c.CreatedBy)
                .Where(c => c.BugId == id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            Console.WriteLine($"Found {comments.Count} comments for bug {id}");

            var commentDTOs = comments.Select(comment => new CommentDTO
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
            }).ToList();

            return commentDTOs;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching comments for bug {id}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            // Instead of 500 Error, we return an empty list
            return Ok(new List<CommentDTO>());
        }
    }

    // POST: api/Bugs/5/comments - Add comment to specific bug
    [HttpPost("{id}/comments")]
    [Authorize]
    public async Task<ActionResult<CommentDTO>> AddCommentToBug(int id, [FromBody] CreateCommentDTO commentDTO)
    {
        // Check if bug exists
        var bug = await _context.Bugs.FindAsync(id);
        if (bug == null)
            return NotFound("Bug not found");

        if (string.IsNullOrWhiteSpace(commentDTO.Text))
            return BadRequest("Comment text is required");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = _userService.GetById(userId);

        var comment = new Comment
        {
            Text = commentDTO.Text,
            BugId = id, // Use the bug ID from the route
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

        return CreatedAtAction("GetComment", "Comments", new { id = comment.Id }, createdCommentDTO);
    }
}