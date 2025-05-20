using Microsoft.AspNetCore.Mvc;
using BugTracker.Api.Data;
using BugTracker.Api.Models;
using BugTracker.API.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BugTracker.API.Service;
using System.Security.Claims;
using System.Reflection.Metadata.Ecma335;

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
        var bugs = await _context.Bugs.Include(b => b.AssignedTo).ToListAsync();

        return bugs;
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<Bug>> GetBug(int id)
    {
        var bug = await _context.Bugs.Include(b => b.AssignedTo).FirstOrDefaultAsync(b => b.Id == id);
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
            AssignedTo = string.IsNullOrEmpty(bugDTO.AssignedToID)? null : _userService.GetById(bugDTO.AssignedToID)
        };


        _context.Bugs.Add(bug);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBug), new { id = bug.Id }, bug);
    }


    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateBug(int id, CreateBugDTO bugDTO)
    {
        var bug = await _context.Bugs.FindAsync(id);

        // update fields from dto
        bug.Title = bugDTO.Title;
        bug.Description = bugDTO.Description;
        bug.Status = bugDTO.Status;   // updatedAt setzen
        bug.UpdatedAt = DateTime.Now;
        bug.AssignedTo = _userService.GetById(bugDTO.AssignedToID);


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
}