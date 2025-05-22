using BugTracker.Api.Models;

namespace BugTracker.API.DTO
{
    public class StatusTransitionDTO
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }

    public class StatusTransitionHistoryDTO
    {
        public int Id { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime TransitionDate { get; set; }
        public UserDTO? ChangedBy { get; set; }
    }

    public class WorkflowInfoDTO
    {
        public string CurrentStatus { get; set; } = string.Empty;
        public List<string> AllowedTransitions { get; set; } = new();
        public List<StatusTransitionHistoryDTO> StatusHistory { get; set; } = new();
    }
}