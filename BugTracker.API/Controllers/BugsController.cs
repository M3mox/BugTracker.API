using Microsoft.AspNetCore.Mvc;
using BugTracker.Api.Data;
using BugTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BugsController : ControllerBase
{
    private readonly BugContext _context;
    public BugsController(BugContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bug>>> GetBugs() =>
        await _context.Bugs.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Bug>> GetBug(int id)
    {
        var bug = await _context.Bugs.FindAsync(id);
        return bug == null ? NotFound() : bug;
    }

    [HttpPost]
    public async Task<ActionResult<Bug>> CreateBug([FromBody] Bug bug)
    {
        Console.WriteLine($"Eingehender Bug: {bug.Title}");

        if (string.IsNullOrWhiteSpace(bug.Title))
            return BadRequest("Titel fehlt");

        bug.CreatedAt = DateTime.Now;
        _context.Bugs.Add(bug);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBug), new { id = bug.Id }, bug);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBug(int id, Bug bug)
    {
        if (id != bug.Id) return BadRequest();

        bug.UpdatedAt = DateTime.UtcNow; // updatedAt setzen
        _context.Entry(bug).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBug(int id)
    {
        var bug = await _context.Bugs.FindAsync(id);
        if (bug == null) return NotFound();
        _context.Bugs.Remove(bug);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}