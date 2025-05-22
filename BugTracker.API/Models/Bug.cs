using BugTracker.API.Models;

namespace BugTracker.Api.Models;

public class Bug
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open"; // String stays for DB-Compability
    public User? CreatedBy { get; set; } = null;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public User? AssignedTo { get; set; }

    // Auxiliary property for enum
    public BugStatus StatusEnum
    {
        get => Enum.TryParse<BugStatus>(Status, out var result) ? result : BugStatus.Open;
        set => Status = value.ToString();
    }
}