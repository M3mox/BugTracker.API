using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BugTracker.Api.Data;
using BugTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Sicherstellen, dass nur authentifizierte User zugreifen
public class BugsController : ControllerBase
{
    private readonly BugContext _context;
    public BugsController(BugContext context) => _context = context;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bug>>> GetBugs() =>
        await _context.Bugs.ToListAsync();

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<Bug>> GetBug(int id)
    {
        var bug = await _context.Bugs.FindAsync(id);
        return bug == null ? NotFound() : bug;
    }

    [HttpPost]
    public async Task<ActionResult<Bug>> CreateBug([FromBody] Bug bug)
    {
        if (string.IsNullOrWhiteSpace(bug.Title))
            return BadRequest("Titel fehlt");

        var createdBy = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(createdBy))
            return Unauthorized();

        bug.CreatedAt = DateTime.Now;
        bug.CreatedBy = createdBy;

        _context.Bugs.Add(bug);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBug), new { id = bug.Id }, bug);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBug(int id, Bug bug)
    {
        if (id != bug.Id) return BadRequest();

        var existingBug = await _context.Bugs.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (existingBug == null) return NotFound();

        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        if (existingBug.CreatedBy != userName)
            return Forbid();

        bug.UpdatedAt = DateTime.UtcNow;
        bug.CreatedBy = existingBug.CreatedBy; // CreatedBy darf nicht geändert werden
        _context.Entry(bug).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBug(int id)
    {
        var bug = await _context.Bugs.FindAsync(id);
        if (bug == null) return NotFound();

        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        if (bug.CreatedBy != userName)
            return Forbid();

        _context.Bugs.Remove(bug);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}