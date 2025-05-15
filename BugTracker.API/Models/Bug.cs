namespace BugTracker.Api.Models;

public class Bug
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Offen";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? AssignedTo { get; set; }

    
    public string CreatedBy { get; set; } = string.Empty;
}
