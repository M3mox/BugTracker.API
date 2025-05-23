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
            Console.WriteLine("=== WORKFLOW TRANSITION DEBUG ===");
            Console.WriteLine($"From Status: {fromStatus}");
            Console.WriteLine($"To Status: {toStatus}");
            Console.WriteLine($"User Role: {userRole}");
            Console.WriteLine($"User ID: {userId}");
            Console.WriteLine($"Bug ID: {bug.Id}");

            // 1. Check if the transition is generally allowed
            if (!_allowedTransitions.ContainsKey(fromStatus))
            {
                Console.WriteLine($"❌ No transitions defined for status {fromStatus}");
                return false;
            }

            if (!_allowedTransitions[fromStatus].Contains(toStatus))
            {
                Console.WriteLine($"❌ Transition from {fromStatus} to {toStatus} is not allowed in workflow");
                Console.WriteLine($"Allowed transitions from {fromStatus}: {string.Join(", ", _allowedTransitions[fromStatus])}");
                return false;
            }

            Console.WriteLine($"✅ Transition {fromStatus} -> {toStatus} is allowed in workflow");

            // 2. Check if the role is authorized
            var transitionKey = (fromStatus, toStatus);
            if (!_rolePermissions.ContainsKey(transitionKey))
            {
                Console.WriteLine($"❌ No role permissions defined for transition {fromStatus} -> {toStatus}");
                return false;
            }

            var allowedRoles = _rolePermissions[transitionKey];
            Console.WriteLine($"Allowed roles for this transition: {string.Join(", ", allowedRoles)}");

            // Admin can do everything
            if (userRole == "admin")
            {
                Console.WriteLine("✅ Admin user - transition allowed");
                return true;
            }

            // Check if user role is authorized for this transition
            if (!allowedRoles.Contains(userRole))
            {
                Console.WriteLine($"❌ User role '{userRole}' is not in allowed roles list");
                return false;
            }

            Console.WriteLine($"✅ User role '{userRole}' is authorized for this transition");

            // Additional check for regular users: User must be creator or assignee
            if (userRole == "user")
            {
                Console.WriteLine("--- Checking user ownership/assignment ---");

                var isCreator = false;
                var isAssigned = false;
                var createdByIdShadow = "";
                var assignedToIdShadow = "";

                // Ansatz 1: Shadow Properties
                try
                {
                    createdByIdShadow = _context.Entry(bug).Property("CreatedById").CurrentValue?.ToString() ?? "";
                    assignedToIdShadow = _context.Entry(bug).Property("AssignedToId").CurrentValue?.ToString() ?? "";

                    Console.WriteLine($"Shadow Property CreatedById: '{createdByIdShadow}'");
                    Console.WriteLine($"Shadow Property AssignedToId: '{assignedToIdShadow}'");

                    if (!string.IsNullOrEmpty(createdByIdShadow))
                    {
                        isCreator = createdByIdShadow == userId;
                    }

                    if (!string.IsNullOrEmpty(assignedToIdShadow))
                    {
                        isAssigned = assignedToIdShadow == userId;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error accessing shadow properties: {ex.Message}");
                }

                // Ansatz 2: Navigation Properties als Fallback
                Console.WriteLine($"Navigation Property CreatedBy: {(bug.CreatedBy != null ? bug.CreatedBy.Id : "null")}");
                Console.WriteLine($"Navigation Property AssignedTo: {(bug.AssignedTo != null ? bug.AssignedTo.Id : "null")}");

                if (!isCreator && bug.CreatedBy != null)
                {
                    isCreator = bug.CreatedBy.Id == userId;
                    Console.WriteLine($"CreatedBy from navigation property: {bug.CreatedBy.Id} == {userId} = {isCreator}");
                }

                if (!isAssigned && bug.AssignedTo != null)
                {
                    isAssigned = bug.AssignedTo.Id == userId;
                    Console.WriteLine($"AssignedTo from navigation property: {bug.AssignedTo.Id} == {userId} = {isAssigned}");
                }

                // Ansatz 3: Direkte Datenbankabfrage als letzter Ausweg
                if (!isCreator && !isAssigned)
                {
                    Console.WriteLine("🔍 Falling back to database query for user validation");
                    try
                    {
                        var bugFromDb = _context.Bugs
                            .Include(b => b.CreatedBy)
                            .Include(b => b.AssignedTo)
                            .FirstOrDefault(b => b.Id == bug.Id);

                        if (bugFromDb != null)
                        {
                            Console.WriteLine($"DB Query CreatedBy: {(bugFromDb.CreatedBy != null ? bugFromDb.CreatedBy.Id : "null")}");
                            Console.WriteLine($"DB Query AssignedTo: {(bugFromDb.AssignedTo != null ? bugFromDb.AssignedTo.Id : "null")}");

                            isCreator = bugFromDb.CreatedBy?.Id == userId;
                            isAssigned = bugFromDb.AssignedTo?.Id == userId;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error with database fallback: {ex.Message}");
                    }
                }

                Console.WriteLine($"Final result - Is Creator: {isCreator}, Is Assigned: {isAssigned}");

                var canTransition = isCreator || isAssigned;
                Console.WriteLine($"User can transition: {canTransition}");

                if (!canTransition)
                {
                    Console.WriteLine("❌ User is neither creator nor assignee - transition denied");
                }
                else
                {
                    Console.WriteLine("✅ User is creator or assignee - transition allowed");
                }

                Console.WriteLine("=== END WORKFLOW DEBUG ===");
                return canTransition;
            }

            Console.WriteLine("✅ Non-user role passed all checks");
            Console.WriteLine("=== END WORKFLOW DEBUG ===");
            return true; // For other roles that passed the role check
        }

        public async Task<bool> TransitionStatusAsync(Bug bug, BugStatus newStatus, string userId, string? comment = null)
        {
            var currentStatus = Enum.Parse<BugStatus>(bug.Status);

            Console.WriteLine($"=== TRANSITION EXECUTION ===");
            Console.WriteLine($"Bug ID: {bug.Id}");
            Console.WriteLine($"From: {currentStatus} -> To: {newStatus}");
            Console.WriteLine($"User: {userId}");
            Console.WriteLine($"Comment: {comment}");

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

            // Mark bug as modified
            _context.Entry(bug).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            Console.WriteLine("✅ Status transition saved successfully");
            Console.WriteLine("=== END TRANSITION EXECUTION ===");
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