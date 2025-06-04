# Bug Tracker 🐞

A comprehensive web-based bug tracking system built with ASP.NET Core Web API and vanilla JavaScript. This application provides a complete workflow management system for tracking and managing software bugs with role-based access control.

## Features

### Core Functionality
- **Bug Management**: Create, edit, update, and delete bug reports
- **User Authentication**: Secure JWT-based login and registration system
- **Role-Based Access Control**: Admin and User roles with different permissions
- **Workflow Management**: Status-based bug lifecycle with controlled transitions
- **Comments System**: Add comments and discussions to bug reports
- **Assignment System**: Assign bugs to specific users

### Workflow States
- **Open**: Newly reported bugs
- **In Progress**: Bugs currently being worked on
- **Testing**: Bugs under testing phase
- **Completed**: Successfully resolved bugs
- **Rejected**: Invalid or duplicate bug reports
- **On Hold**: Temporarily paused bugs
- **Failed**: Bugs that failed testing
- **Reopened**: Previously completed bugs that need attention

### User Interface
- **Dashboard**: Overview of all bugs with filtering and search capabilities
- **Status Overview**: Visual representation of bug distribution across statuses
- **Quick Actions**: Fast status transitions directly from the dashboard
- **Responsive Design**: Mobile-friendly interface using Tailwind CSS

## Technology Stack

### Backend
- **ASP.NET Core 6.0** - Web API framework
- **Entity Framework Core** - ORM for database operations
- **SQL Server** - Primary database
- **JWT Authentication** - Secure token-based authentication
- **Identity Framework** - User management and password hashing

### Frontend
- **Vanilla JavaScript** - No framework dependencies
- **Tailwind CSS** - Utility-first CSS framework
- **SweetAlert2** - Beautiful alert dialogs
- **HTML5** - Semantic markup


## Security Features

- **JWT Token Authentication**: Secure stateless authentication
- **Password Hashing**: BCrypt-based password security
- **Role-Based Authorization**: Granular permission control
- **CORS Configuration**: Controlled cross-origin requests
- **Input Validation**: Server-side validation for all inputs


## Prerequisites

- .NET 6.0 SDK or later
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 or VS Code
- Modern web browser

## Installation & Setup

### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/bug-tracker.git
cd bug-tracker
```

### 2. Configure Database Connection
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BugTrackerDb;Trusted_Connection=true;"
  }
}
```

### 3. Configure JWT Settings
Set up JWT configuration in `appsettings.json` or user secrets:
```json
{
  "Jwt": {
    "Key": "your-super-secret-jwt-key-here-minimum-32-characters",
    "Issuer": "BugTracker",
    "Audience": "BugTracker"
  }
}
```

### 4. Restore Dependencies
```bash
dotnet restore
```

### 5. Run Database Migrations
The application uses `EnsureCreated()` to automatically create the database on first run.

### 6. Start the Application
```bash
dotnet run
```

The API will be available at `https://localhost:7063` and the web interface at the same URL.

## Usage

### Getting Started
1. **Register a new account** at `/register.html`
2. **Login** with your credentials at `/login.html`
3. **Create your first bug** using the "Create Ticket" button
4. **Manage workflows** through the Workflow page

### User Roles

#### Regular User
- Create and edit own bugs
- Comment on bugs
- Transition bug statuses (limited permissions)
- View assigned bugs

#### Admin User
- All user permissions
- Delete any bug
- Perform any status transition
- Access workflow statistics
- Manage all bugs regardless of assignment


## Development

### Running in Development
```bash
dotnet run --environment Development
```

### Building for Production
```bash
dotnet publish -c Release -o ./publish
```

### Database Migrations
The application automatically creates and updates the database schema on startup using Entity Framework's `EnsureCreated()` method.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](License) file for details.

## Support

If you encounter any issues or have questions, please:
1. Check the existing issues on GitHub
2. Create a new issue with detailed information
3. Include steps to reproduce any bugs

## Roadmap (Features to come)

- [ ] Email notifications for status changes
- [ ] File attachments for bugs
- [ ] Advanced reporting and analytics
- [ ] Integration with external tools (Slack, Teams)
- [ ] Mobile app development
- [ ] Advanced workflow customization

---

**Made with ❤️ using ASP.NET Core and vanilla JavaScript by Miriam Huber**
