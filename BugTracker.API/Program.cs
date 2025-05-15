using BugTracker.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace BugTracker.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- JWT-Konfiguration auslesen ---
            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
                throw new InvalidOperationException("JWT Key is missing or too short (min. 32 characters).");

            // --- Services konfigurieren ---
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

            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<BugContext>()
                .AddDefaultTokenProviders();

            // --- JWT Authentifizierung konfigurieren ---
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                        IssuerSigningKey = key,
                        ClockSkew = TimeSpan.Zero, // Verhindert Zeitabweichungen

                        // WICHTIG: Diese beiden Zeilen sorgen für korrekte Füllung von User.Identity.Name und Rollen
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"JWT Auth failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        }
                    };
                });

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

            app.UseAuthentication(); // Muss vor Authorization
            app.UseAuthorization();

            app.MapControllers();

            // --- Datenbank sicherstellen ---
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BugContext>();
                db.Database.EnsureCreated();
            }

            app.Run();
        }
    }
}
