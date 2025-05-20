using BugTracker.API.Models;
using BugTracker.Api.Controllers;
using BugTracker.API.DTO;
using BugTracker.Api.Data;

namespace BugTracker.API.Service
{
    public class UserService
    {
        private readonly BugContext _context;

        public UserService (BugContext context)
        {
            _context = context;
        }
        public List<User> GetUsers()
        {
            return _context.User.ToList(); 
            
        }

        public User GetUser(string userName, string password)
        {
            var user = GetUsers().FirstOrDefault(u =>
                u.Username == userName && u.Password == password);
            return user;
        }

        public User GetById(string assignedToID)
        {
            var user = GetUsers().FirstOrDefault(u => u.Id == assignedToID);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            return user;
        }

    }
}
