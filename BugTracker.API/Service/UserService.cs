using BugTracker.API.Models;
using BugTracker.Api.Controllers;
using BugTracker.API.DTO;
using BugTracker.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BugTracker.API.Service
{
    public class UserService
    {
        private readonly BugContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(BugContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public List<User> GetUsers()
        {
            return _context.User.ToList();
        }

        public User GetUser(string userName, string password)
        {
            // Find the user first (without password verification)
            var user = GetUsers().FirstOrDefault(u => u.Username == userName);

            if (user == null)
                return null;

            // verify Password
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, password);

            // If the password matches, return the user
            if (passwordVerificationResult == PasswordVerificationResult.Success)
                return user;

            // Return null if password is incorrect
            return null;
        }

        public User GetById(string assignedToID)
        {
            var user = GetUsers().FirstOrDefault(u => u.Id == assignedToID);
            if (user == null)
                throw new InvalidOperationException("User not found.");
            return user;
        }

        // Method for creating a user with a hashed password
        public User CreateUser(string username, string password, string role = "user")
        {
            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Role = role,
                // Password remains empty, is hashed immediately after creation
                Password = ""
            };

            // hashing Password
            newUser.Password = _passwordHasher.HashPassword(newUser, password);

            // Add users to the database
            _context.User.Add(newUser);
            _context.SaveChanges();

            return newUser;
        }

        // Updating a password
        public bool UpdatePassword(string userId, string newPassword)
        {
            var user = GetById(userId);
            if (user == null)
                return false;

            // Hash and save new password
            user.Password = _passwordHasher.HashPassword(user, newPassword);
            _context.SaveChanges();

            return true;
        }

        // Secure deletion of users
        public async Task<bool> DeleteUserSafeAsync(string userId)
        {
            var user = _context.User.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            // Check if the user is still referenced in bugs
            var hasBugsAsCreator = await _context.Bugs.AnyAsync(b => b.CreatedBy != null && b.CreatedBy.Id == userId);
            var hasBugsAsAssigned = await _context.Bugs.AnyAsync(b => b.AssignedTo != null && b.AssignedTo.Id == userId);

            // Check if the user is still referenced in comments
            var hasComments = await _context.Comments.AnyAsync(c => c.CreatedBy != null && c.CreatedBy.Id == userId);

            if (hasBugsAsCreator || hasBugsAsAssigned || hasComments)
            {
                // User cannot be deleted because it is still referenced
                return false;
            }

            // User can be safely deleted
            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // deactivate user instead of deleting
        public async Task<bool> DeactivateUserAsync(string userId)
        {
            var user = _context.User.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            
            
            user.Username = $"[DELETED]_{user.Username}_{DateTime.UtcNow.Ticks}";

            await _context.SaveChangesAsync();
            return true;
        }

        // Helper method for migrating existing users to hashed passwords
        public void MigrateUsersToHashedPasswords()
        {
            var users = GetUsers();
            foreach (var user in users)
            {
                // Only migrate if the password is not yet hashed              
                if (!user.Password.StartsWith("AQAAAA"))
                {
                    var plainPassword = user.Password; // Remember original password
                    user.Password = _passwordHasher.HashPassword(user, plainPassword);
                }
            }
            _context.SaveChanges();
        }
    }
}