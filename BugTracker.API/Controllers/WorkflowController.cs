using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BugTracker.API.Service;
using BugTracker.Api.Models;
using BugTracker.API.DTO;
using System.Security.Claims;

namespace BugTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkflowController : ControllerBase
    {
        private readonly BugWorkflowService _workflowService;

        public WorkflowController(BugWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        // GET: api/Workflow/statuses - Get all available bug statuses
        [HttpGet("statuses")]
        public ActionResult<List<object>> GetAllStatuses()
        {
            var statuses = Enum.GetValues<BugStatus>()
                .Select(status => new
                {
                    Value = status.ToString(),
                    DisplayName = BugWorkflowService.GetStatusDisplayName(status),
                    Description = GetStatusDescription(status)
                })
                .ToList();

            return Ok(statuses);
        }

        // GET: api/Workflow/transitions/{fromStatus} - Get allowed transitions from a status
        [HttpGet("transitions/{fromStatus}")]
        public ActionResult<List<string>> GetAllowedTransitions(string fromStatus)
        {
            if (!Enum.TryParse<BugStatus>(fromStatus, out var status))
            {
                return BadRequest("Invalid status");
            }

            var allowedTransitions = _workflowService.GetAllowedTransitions(status)
                .Select(s => BugWorkflowService.GetStatusDisplayName(s))
                .ToList();

            return Ok(allowedTransitions);
        }

        // GET: api/Workflow/diagram - Get workflow diagram data
        [HttpGet("diagram")]
        public ActionResult<object> GetWorkflowDiagram()
        {
            var workflow = new
            {
                Nodes = Enum.GetValues<BugStatus>().Select(status => new
                {
                    Id = status.ToString(),
                    Label = BugWorkflowService.GetStatusDisplayName(status),
                    Description = GetStatusDescription(status),
                    Color = GetStatusColor(status)
                }).ToList(),

                Edges = GetAllTransitionEdges()
            };

            return Ok(workflow);
        }

        // GET: api/Workflow/statistics - Get workflow statistics (Admin only)
        [HttpGet("statistics")]
        [Authorize(Roles = "admin")]
        public ActionResult<object> GetWorkflowStatistics()
        {
            // This would require additional database queries
            // For now, return placeholder data
            var stats = new
            {
                TotalBugs = 0,
                StatusDistribution = Enum.GetValues<BugStatus>().ToDictionary(
                    status => BugWorkflowService.GetStatusDisplayName(status),
                    status => 0 // Would be actual count from database
                ),
                AverageTimeInStatus = new Dictionary<string, double>(),
                MostCommonTransitions = new List<object>()
            };

            return Ok(stats);
        }

        private static string GetStatusDescription(BugStatus status)
        {
            return status switch
            {
                BugStatus.Open => "Bug has been reported and is waiting to be addressed",
                BugStatus.InProgress => "Bug is currently being worked on",
                BugStatus.Testing => "Bug fix is being tested",
                BugStatus.Completed => "Bug has been resolved and tested",
                BugStatus.Rejected => "Bug report was deemed invalid or duplicate",
                BugStatus.OnHold => "Work on bug is temporarily paused",
                BugStatus.Failed => "Bug fix failed testing and needs more work",
                BugStatus.Reopened => "Previously completed bug has been reopened",
                _ => "Unknown status"
            };
        }

        private static string GetStatusColor(BugStatus status)
        {
            return status switch
            {
                BugStatus.Open => "#3B82F6", // Blue
                BugStatus.InProgress => "#F59E0B", // Amber
                BugStatus.Testing => "#8B5CF6", // Purple
                BugStatus.Completed => "#10B981", // Green
                BugStatus.Rejected => "#EF4444", // Red
                BugStatus.OnHold => "#6B7280", // Gray
                BugStatus.Failed => "#DC2626", // Dark Red
                BugStatus.Reopened => "#F97316", // Orange
                _ => "#374151" // Dark Gray
            };
        }

        private List<object> GetAllTransitionEdges()
        {
            var edges = new List<object>();

            var transitions = new Dictionary<BugStatus, List<BugStatus>>
            {
                [BugStatus.Open] = new() { BugStatus.InProgress, BugStatus.Rejected },
                [BugStatus.InProgress] = new() { BugStatus.Testing, BugStatus.OnHold, BugStatus.Open },
                [BugStatus.Testing] = new() { BugStatus.Completed, BugStatus.Failed },
                [BugStatus.Completed] = new() { BugStatus.Reopened },
                [BugStatus.Rejected] = new() { BugStatus.Reopened },
                [BugStatus.OnHold] = new() { BugStatus.InProgress, BugStatus.Rejected },
                [BugStatus.Failed] = new() { BugStatus.InProgress },
                [BugStatus.Reopened] = new() { BugStatus.InProgress, BugStatus.Rejected }
            };

            foreach (var (from, toList) in transitions)
            {
                foreach (var to in toList)
                {
                    edges.Add(new
                    {
                        From = from.ToString(),
                        To = to.ToString(),
                        Label = $"{BugWorkflowService.GetStatusDisplayName(from)} → {BugWorkflowService.GetStatusDisplayName(to)}"
                    });
                }
            }

            return edges;
        }
    }
}