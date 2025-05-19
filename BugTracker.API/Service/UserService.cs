using BugTracker.API.Models;

namespace BugTracker.API.Service
{
    public class UserService
    {
        public List<User> GetUsers()
        {
            // Beispiel-User
            var users = new List<User>
            {
                new User { Username = "admin", Password = "admin123", Role = "admin" },
                new User { Username = "employee", Password = "user123", Role = "employee" }
            };
            return users;
        }

        public User GetUser(string userName, string password)
        {
            var user = GetUsers().FirstOrDefault(u =>
                u.Username == userName && u.Password == password);
            return user;
        }
    }
}
