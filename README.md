🐞 Bug Tracker

A modern, lightweight bug tracking application built with HTML, JavaScript, and a .NET API backend.
Bild anzeigen

📋 Features

User Authentication: Secure login and registration system
Ticket Management: Create, view, update, and delete bug tickets
User Assignment: Assign tickets to specific users
Role-Based Access Control: Admin and regular user roles with different permissions
Responsive Design: Works on desktop and mobile devices

🔧 Technologies Used

Frontend:

HTML5
CSS3 with Tailwind CSS
JavaScript (ES6+)
SweetAlert2 for notifications


Backend:

ASP.NET Core Web API (.NET 6)
JWT Authentication
Entity Framework Core



🚀 Getting Started
Prerequisites

.NET 6 SDK or later
Web browser (Chrome, Firefox, Edge, etc.)
A local development environment or web server

Installation

Clone the repository

bashgit clone https://github.com/yourusername/bug-tracker.git
cd bug-tracker

Start the backend API and frontend

bashdotnet restore
dotnet run
The application will start and be available at https://localhost:7063.
You can access the frontend by navigating to this URL in your browser.
🔍 Usage
Login / Registration

Open login.html in your browser
Login with your credentials or click "Need an account? Register" to create a new account
New users will be assigned the "user" role by default

Creating Tickets

After logging in, you'll be redirected to the dashboard
Fill out the ticket creation form with title, description, and status
Optionally assign the ticket to a user
Click "Create Ticket" to submit

Managing Tickets

View Details: Click on any ticket row to view full details
Edit: If you're an admin or the ticket creator, you can edit tickets using the "Edit" button
Delete: Admins can delete tickets using the "Delete" button

👥 User Roles

User: Can create and manage their own tickets, assign tickets to other users
Admin: Additional permissions to edit/delete any ticket, indicated by an "Admin" badge

📁 Project Structure

/

├── Program.cs              # Main application entry point

│
├── Controllers/            # API Controllers

│   ├── AuthController.cs   # Authentication endpoints

│   ├── BugController.cs    # Bug ticket endpoints

│   └── UsersController.cs  # User management endpoints

│

├── Models/                 # Data models

│   ├── Bug.cs              # Bug ticket model

│   └── User.cs             # User model

│

├── DTO/                    # Data Transfer Objects

│   ├── BugDTO.cs           # Bug DTO

│   └── UserDTO.cs          # User DTO

│

├── Data/                   # Database context

│   └── BugContext.cs       # EF Core DB context

│

├── Service/               # Business logic services

│   └── UserService.cs      # User management service

└── wwwroot/                # Static web files (frontend)

│   └── index.html          # Main dashboard

│     

│    └── login.html          # Login page

│   └── register.html       # Registration page

│   ├── script.js           # Main dashboard script

│   ├── login.js            # Login page script

│   ├── register.js         # Registration page script

│

├── css/                # Stylesheet folder

│   └── style.css       # CSS styles

│

└── favicon files       # Various favicon formats
    
    
🔐 Authentication Flow

User registers or logs in through the web interface
The API validates credentials and returns a JWT token
The token is stored in the browser's localStorage
All subsequent API requests include this token
The application decodes the token to determine user roles and permissions

⚠️ Known Issues and Limitations

Session expires after token timeout (currently set to 24 hours)
Limited filtering and searching capabilities for tickets
No email notifications for ticket assignments

🛠️ Future Improvements

 Implement advanced search and filtering
 Add comment functionality to tickets
 Email notifications for ticket updates
 Dark mode support
 Export tickets to CSV/PDF

📄 License
This project is licensed under the MIT License - see the LICENSE file for details.
🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

Fork the repository
Create your feature branch (git checkout -b feature/amazing-feature)
Commit your changes (git commit -m 'Add some amazing feature')
Push to the branch (git push origin feature/amazing-feature)
Open a Pull Request


Created by Miriam Huber
