using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BugTracker.Api.Models;

namespace BugTracker.Api.Data;

public class BugContext : IdentityDbContext<IdentityUser>
{
    public BugContext(DbContextOptions<BugContext> options) : base(options) { }

    public DbSet<Bug> Bugs => Set<Bug>();
}