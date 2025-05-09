using Microsoft.EntityFrameworkCore;
using BugTracker.Api.Models;

namespace BugTracker.Api.Data;

public class BugContext : DbContext
{
    public BugContext(DbContextOptions<BugContext> options) : base(options) { }

    public DbSet<Bug> Bugs => Set<Bug>();
}
