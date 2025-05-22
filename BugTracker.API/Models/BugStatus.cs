using System.ComponentModel;

namespace BugTracker.Api.Models
{
    public enum BugStatus
    {
        [Description("Open")]
        Open = 0,

        [Description("In Progress")]
        InProgress = 1,

        [Description("Testing")]
        Testing = 2,

        [Description("Completed")]
        Completed = 3,

        [Description("Rejected")]
        Rejected = 4,

        [Description("On Hold")]
        OnHold = 5,

        [Description("Failed")]
        Failed = 6,

        [Description("Reopened")]
        Reopened = 7
    }
}