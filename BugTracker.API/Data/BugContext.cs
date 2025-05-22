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
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Comment Konfiguration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);

            // Bug Beziehung
            entity.HasOne(c => c.Bug)
                  .WithMany()
                  .HasForeignKey(c => c.BugId)
                  .OnDelete(DeleteBehavior.Cascade);

            // User Beziehung (CreatedBy)
            entity.HasOne(c => c.CreatedBy)
                  .WithMany()
                  .HasForeignKey(c => c.CreatedById)
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
        });

        // Bug Konfiguration
        modelBuilder.Entity<Bug>(entity =>
        {
            entity.HasKey(b => b.Id);

            // CreatedBy Beziehung
            entity.HasOne(b => b.CreatedBy)
                  .WithMany()
                  .HasForeignKey("CreatedById")
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);

            // AssignedTo Beziehung  
            entity.HasOne(b => b.AssignedTo)
                  .WithMany()
                  .HasForeignKey("AssignedToId")
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
        });

        // User Konfiguration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
        });
    }
}