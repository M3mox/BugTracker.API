using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BugTracker.Api.Models;
using BugTracker.API.Models;

namespace BugTracker.Api.Data;

public class BugContext : IdentityDbContext<IdentityUser>
{
    public BugContext(DbContextOptions<BugContext> options) : base(options) { }

    public DbSet<Bug> Bugs => Set<Bug>();
    public DbSet<User> User => Set<User>();
}