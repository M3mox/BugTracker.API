using BugTracker.API.Models;

namespace BugTracker.API.DTO
{
    public class CreateCommentDTO
    {
        public string Text { get; set; } = string.Empty;
        public int BugId { get; set; }
    }

    public class CommentDTO
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int BugId { get; set; }
        public UserDTO? CreatedBy { get; set; }
    }
}