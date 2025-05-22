using BugTracker.Api.Models;
using BugTracker.Api.Data;
using BugTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.API.Service
{
    public class BugWorkflowService
    {
        private readonly BugContext _context;

        public BugWorkflowService(BugContext context)
        {
            _context = context;
        }

        // Defines the allowed status transitions
        private readonly Dictionary<BugStatus, List<BugStatus>> _allowedTransitions = new()
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

        // Role-based permissions for status transitions
        private readonly Dictionary<(BugStatus From, BugStatus To), List<string>> _rolePermissions = new()
        {
            // Anyone can go from Open to InProgress
            [(BugStatus.Open, BugStatus.InProgress)] = new() { "user", "admin" },
            [(BugStatus.Open, BugStatus.Rejected)] = new() { "admin" },

            // Assigned User or Admin can proceed from InProgress
            [(BugStatus.InProgress, BugStatus.Testing)] = new() { "user", "admin" },
            [(BugStatus.InProgress, BugStatus.OnHold)] = new() { "user", "admin" },
            [(BugStatus.InProgress, BugStatus.Open)] = new() { "user", "admin" },

            // Only Admin or Tester can complete Testing
            [(BugStatus.Testing, BugStatus.Completed)] = new() { "admin" },
            [(BugStatus.Testing, BugStatus.Failed)] = new() { "admin" },

            // Only Admin can reopen Completed/Rejected
            [(BugStatus.Completed, BugStatus.Reopened)] = new() { "admin" },
            [(BugStatus.Rejected, BugStatus.Reopened)] = new() { "admin" },

            // OnHold transitions
            [(BugStatus.OnHold, BugStatus.InProgress)] = new() { "user", "admin" },
            [(BugStatus.OnHold, BugStatus.Rejected)] = new() { "admin" },

            // Failed back to InProgress
            [(BugStatus.Failed, BugStatus.InProgress)] = new() { "user", "admin" },

            // Reopened transitions
            [(BugStatus.Reopened, BugStatus.InProgress)] = new() { "user", "admin" },
            [(BugStatus.Reopened, BugStatus.Rejected)] = new() { "admin" }
        };

        public List<BugStatus> GetAllowedTransitions(BugStatus currentStatus)
        {
            return _allowedTransitions.ContainsKey(currentStatus)
                ? _allowedTransitions[currentStatus]
                : new List<BugStatus>();
        }

        public bool IsTransitionAllowed(BugStatus fromStatus, BugStatus toStatus, string userRole, Bug bug, string userId)
        {
            // 1. Check if the transition is generally allowed
            if (!_allowedTransitions.ContainsKey(fromStatus) ||
                !_allowedTransitions[fromStatus].Contains(toStatus))
            {
                return false;
            }

            // 2. Check if the role is authorized
            var transitionKey = (fromStatus, toStatus);
            if (!_rolePermissions.ContainsKey(transitionKey))
            {
                return false;
            }

            var allowedRoles = _rolePermissions[transitionKey];

            // Admin can do everything
            if (userRole == "admin")
            {
                return true;
            }

            // Check if user is authorized
            if (!allowedRoles.Contains(userRole))
            {
                return false;
            }

            // Additional check: User must be creator or assignee
            bool isCreator = bug.CreatedBy?.Id == userId;
            bool isAssigned = bug.AssignedTo?.Id == userId;

            return isCreator || isAssigned;
        }

        public async Task<bool> TransitionStatusAsync(Bug bug, BugStatus newStatus, string userId, string? comment = null)
        {
            var currentStatus = Enum.Parse<BugStatus>(bug.Status);

            // Store status transition in history
            var transition = new StatusTransition
            {
                BugId = bug.Id,
                FromStatus = currentStatus,
                ToStatus = newStatus,
                Comment = comment,
                TransitionDate = DateTime.UtcNow,
                ChangedById = userId
            };

            _context.StatusTransitions.Add(transition);

            // Update bug status
            bug.Status = newStatus.ToString();
            bug.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<StatusTransition>> GetStatusHistoryAsync(int bugId)
        {
            return await _context.StatusTransitions
                .Where(st => st.BugId == bugId)
                .Include(st => st.ChangedBy)
                .OrderBy(st => st.TransitionDate)
                .ToListAsync();
        }

        public static string GetStatusDisplayName(BugStatus status)
        {
            return status switch
            {
                BugStatus.Open => "Open",
                BugStatus.InProgress => "In Progress",
                BugStatus.Testing => "Testing",
                BugStatus.Completed => "Completed",
                BugStatus.Rejected => "Rejected",
                BugStatus.OnHold => "On Hold",
                BugStatus.Failed => "Failed",
                BugStatus.Reopened => "Reopened",
                _ => status.ToString()
            };
        }
    }
}