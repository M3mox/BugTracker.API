using BugTracker.Api.Data;
using BugTracker.API.Models;
using BugTracker.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BugTracker.API.Service;

var builder = WebApplication.CreateBuilder(args);

// --- Read JWT config ---
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT Key is missing. Set it in environment variables or user secrets.");

// --- Configure services ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

builder.Services.AddDbContext<BugContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<BugContext>()
    .AddDefaultTokenProviders();

// JWT Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        if (keyBytes.Length < 32)
        {
            var extended = new byte[32];
            Array.Copy(keyBytes, extended, keyBytes.Length);
            keyBytes = extended;
        }
        var key = new SymmetricSecurityKey(keyBytes);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = key
        };
    });

// Add Password Hasher for our User model
builder.Services.AddTransient<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<BugWorkflowService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowAll");
app.UseAuthentication(); // <<< Important: Before Authorization
app.UseAuthorization();

// --- Ensure DB exists and migrate passwords ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BugContext>();

    try
    {
        // ONLY ensure that the DB exists (WITHOUT deleting!)
        Console.WriteLine("Ensuring database exists...");
        db.Database.EnsureCreated();

        Console.WriteLine("Database ensured successfully");

        // Test if the Comments table exists
        var commentsExist = db.Database.CanConnect() &&
                           db.Model.FindEntityType(typeof(Comment)) != null;
        Console.WriteLine($"Comments table exists: {commentsExist}");

        // Migrate existing users to hashed passwords 
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        userService.MigrateUsersToHashedPasswords();
        Console.WriteLine("User password migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during database initialization: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");

        // If initialization fails, try again
        try
        {
            Console.WriteLine("Trying fallback database creation...");
            db.Database.EnsureCreated();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            userService.MigrateUsersToHashedPasswords();
            Console.WriteLine("Fallback database creation successful");
        }
        catch (Exception fallbackEx)
        {
            Console.WriteLine($"Fallback also failed: {fallbackEx.Message}");
        }
    }
}

app.MapControllers();
app.Run();