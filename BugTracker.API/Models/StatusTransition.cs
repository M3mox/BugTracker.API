using BugTracker.API.Models;

namespace BugTracker.Api.Models
{
    public class StatusTransition
    {
        public int Id { get; set; }
        public int BugId { get; set; }
        public Bug Bug { get; set; } = null!;

        public BugStatus FromStatus { get; set; }
        public BugStatus ToStatus { get; set; }

        public string? Comment { get; set; }
        public DateTime TransitionDate { get; set; } = DateTime.UtcNow;

        // Who made the transition
        public string? ChangedById { get; set; }
        public User? ChangedBy { get; set; } = null;
    }
}