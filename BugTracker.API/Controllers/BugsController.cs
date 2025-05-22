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
    private readonly BugWorkflowService _workflowService;

    public BugsController(BugContext context, UserService userService, BugWorkflowService workflowService)
    {
        _context = context;
        _userService = userService;
        _workflowService = workflowService;
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
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "user";
        var isAdmin = userRole == "admin";

        // Check if user is authorized to update this bug
        // User can edit if: Admin OR Created the bug OR Assigned to the bug
        bool canEdit = isAdmin ||
                      (bug.CreatedBy != null && bug.CreatedBy.Id == userId) ||
                      (bug.AssignedTo != null && bug.AssignedTo.Id == userId);

        if (!canEdit)
            return Forbid("You can only edit bugs you created or bugs assigned to you.");

        // Handle status change through workflow if status is different
        var currentStatus = bug.Status;
        var newStatusString = bugDTO.Status;

        if (currentStatus != newStatusString)
        {
            // Parse statuses
            if (!Enum.TryParse<BugStatus>(currentStatus, out var currentStatusEnum) ||
                !Enum.TryParse<BugStatus>(newStatusString, out var newStatusEnum))
            {
                return BadRequest("Invalid status value");
            }

            // Check if status transition is allowed
            if (!_workflowService.IsTransitionAllowed(currentStatusEnum, newStatusEnum, userRole, bug, userId))
            {
                return Forbid($"Status transition from {BugWorkflowService.GetStatusDisplayName(currentStatusEnum)} to {BugWorkflowService.GetStatusDisplayName(newStatusEnum)} is not allowed for your role.");
            }

            // Perform workflow transition
            await _workflowService.TransitionStatusAsync(bug, newStatusEnum, userId, "Status updated via bug edit");
        }

        // Update other fields
        bug.Title = bugDTO.Title;
        bug.Description = bugDTO.Description;
        // Status is already updated through workflow if changed
        if (currentStatus == newStatusString)
        {
            bug.UpdatedAt = DateTime.Now;
        }

        // Only update assignedTo if a value was provided
        if (!string.IsNullOrEmpty(bugDTO.AssignedToID))
        {
            bug.AssignedTo = _userService.GetById(bugDTO.AssignedToID);
        }

        // Save (workflow transition already saved status)
        if (currentStatus == newStatusString)
        {
            _context.Entry(bug).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

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

            // Prüfen, ob die Comments-Tabelle existiert
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
            // Statt 500 Error, geben wir eine leere Liste zurück
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

    // WORKFLOW ENDPOINTS

    // GET: api/Bugs/5/workflow - Get workflow info for a bug
    [HttpGet("{id}/workflow")]
    [Authorize]
    public async Task<ActionResult<WorkflowInfoDTO>> GetBugWorkflowInfo(int id)
    {
        var bug = await _context.Bugs
            .Include(b => b.CreatedBy)
            .Include(b => b.AssignedTo)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bug == null)
            return NotFound("Bug not found");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "user";

        var currentStatus = Enum.Parse<BugStatus>(bug.Status);
        var allowedTransitions = _workflowService.GetAllowedTransitions(currentStatus);

        // Filter transitions based on user permissions
        var permittedTransitions = allowedTransitions
            .Where(transition => _workflowService.IsTransitionAllowed(currentStatus, transition, userRole, bug, userId))
            .Select(status => BugWorkflowService.GetStatusDisplayName(status))
            .ToList();

        var statusHistory = await _workflowService.GetStatusHistoryAsync(id);
        var statusHistoryDTOs = statusHistory.Select(sh => new StatusTransitionHistoryDTO
        {
            Id = sh.Id,
            FromStatus = BugWorkflowService.GetStatusDisplayName(sh.FromStatus),
            ToStatus = BugWorkflowService.GetStatusDisplayName(sh.ToStatus),
            Comment = sh.Comment,
            TransitionDate = sh.TransitionDate,
            ChangedBy = sh.ChangedBy != null ? new UserDTO
            {
                Id = sh.ChangedBy.Id,
                Username = sh.ChangedBy.Username,
                Role = sh.ChangedBy.Role
            } : null
        }).ToList();

        var workflowInfo = new WorkflowInfoDTO
        {
            CurrentStatus = BugWorkflowService.GetStatusDisplayName(currentStatus),
            AllowedTransitions = permittedTransitions,
            StatusHistory = statusHistoryDTOs
        };

        return workflowInfo;
    }

    // POST: api/Bugs/5/transition - Transition bug status
    [HttpPost("{id}/transition")]
    [Authorize]
    public async Task<IActionResult> TransitionBugStatus(int id, [FromBody] StatusTransitionDTO transitionDTO)
    {
        try
        {
            var bug = await _context.Bugs
                .Include(b => b.CreatedBy)
                .Include(b => b.AssignedTo)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bug == null)
                return NotFound("Bug not found");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "user";

            // Parse new status
            if (!Enum.TryParse<BugStatus>(transitionDTO.NewStatus, out var newStatus))
            {
                return BadRequest("Invalid status");
            }

            var currentStatus = Enum.Parse<BugStatus>(bug.Status);

            // Check if transition is allowed
            if (!_workflowService.IsTransitionAllowed(currentStatus, newStatus, userRole, bug, userId))
            {
                return Forbid("You are not authorized to perform this status transition");
            }

            // Perform the transition
            await _workflowService.TransitionStatusAsync(bug, newStatus, userId, transitionDTO.Comment);

            return Ok(new { message = "Status transition successful", newStatus = BugWorkflowService.GetStatusDisplayName(newStatus) });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during status transition: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error during status transition" });
        }
    }

    // GET: api/Bugs/5/status-history - Get status history for a bug
    [HttpGet("{id}/status-history")]
    [Authorize]
    public async Task<ActionResult<List<StatusTransitionHistoryDTO>>> GetBugStatusHistory(int id)
    {
        var bugExists = await _context.Bugs.AnyAsync(b => b.Id == id);
        if (!bugExists)
            return NotFound("Bug not found");

        var statusHistory = await _workflowService.GetStatusHistoryAsync(id);
        var statusHistoryDTOs = statusHistory.Select(sh => new StatusTransitionHistoryDTO
        {
            Id = sh.Id,
            FromStatus = BugWorkflowService.GetStatusDisplayName(sh.FromStatus),
            ToStatus = BugWorkflowService.GetStatusDisplayName(sh.ToStatus),
            Comment = sh.Comment,
            TransitionDate = sh.TransitionDate,
            ChangedBy = sh.ChangedBy != null ? new UserDTO
            {
                Id = sh.ChangedBy.Id,
                Username = sh.ChangedBy.Username,
                Role = sh.ChangedBy.Role
            } : null
        }).ToList();

        return statusHistoryDTOs;
    }
}