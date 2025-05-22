using BugTracker.API.Models;

namespace BugTracker.Api.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Key zu Bug
        public int BugId { get; set; }
        public Bug Bug { get; set; } = null!;

        // Explicit Foreign Key für User
        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; } = null;
    }
}