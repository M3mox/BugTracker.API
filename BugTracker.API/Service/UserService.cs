using BugTracker.API.Models;
using BugTracker.Api.Controllers;
using BugTracker.API.DTO;
using BugTracker.Api.Data;
using Microsoft.AspNetCore.Identity;
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
            // Zuerst den Benutzer finden (ohne Passwortprüfung)
            var user = GetUsers().FirstOrDefault(u => u.Username == userName);

            if (user == null)
                return null;

            // Passwort überprüfen
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, password);

            // Wenn das Passwort übereinstimmt, geben wir den Benutzer zurück
            if (passwordVerificationResult == PasswordVerificationResult.Success)
                return user;

            // Bei falschem Passwort null zurückgeben
            return null;
        }

        public User GetById(string assignedToID)
        {
            var user = GetUsers().FirstOrDefault(u => u.Id == assignedToID);
            if (user == null)
                throw new InvalidOperationException("User not found.");
            return user;
        }

        // Neue Methode zum Erstellen eines Benutzers mit gehashtem Passwort
        public User CreateUser(string username, string password, string role = "user")
        {
            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Role = role,
                // Das Passwort bleibt leer, wird direkt nach der Erstellung gehasht
                Password = ""
            };

            // Passwort hashen
            newUser.Password = _passwordHasher.HashPassword(newUser, password);

            // Benutzer in die Datenbank einfügen
            _context.User.Add(newUser);
            _context.SaveChanges();

            return newUser;
        }

        // Methode zum Aktualisieren eines Passworts
        public bool UpdatePassword(string userId, string newPassword)
        {
            var user = GetById(userId);
            if (user == null)
                return false;

            // Neues Passwort hashen und speichern
            user.Password = _passwordHasher.HashPassword(user, newPassword);
            _context.SaveChanges();

            return true;
        }

        // Hilfsmethode zum Migrieren vorhandener Benutzer zu gehashten Passwörtern
        public void MigrateUsersToHashedPasswords()
        {
            var users = GetUsers();
            foreach (var user in users)
            {
                // Nur migrieren, wenn das Passwort noch nicht gehasht ist
                // (Dies ist eine vereinfachte Erkennung und könnte in der Praxis verbessert werden)
                if (!user.Password.StartsWith("AQAAAA"))
                {
                    var plainPassword = user.Password; // Originalpasswort merken
                    user.Password = _passwordHasher.HashPassword(user, plainPassword);
                }
            }
            _context.SaveChanges();
        }
    }
}