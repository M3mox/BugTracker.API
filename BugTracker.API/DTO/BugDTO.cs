using BugTracker.API.Models;

namespace BugTracker.API.DTO
{
    public class CreateBugDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Offen";
        public string? AssignedToID { get; set; }
    }
}
