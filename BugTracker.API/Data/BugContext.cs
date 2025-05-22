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
    public DbSet<StatusTransition> StatusTransitions => Set<StatusTransition>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Comment Configuration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            // Bug Relationship
            entity.HasOne(c => c.Bug)
                  .WithMany()
                  .HasForeignKey(c => c.BugId)
                  .OnDelete(DeleteBehavior.Cascade);
            // User Relationship (CreatedBy)
            entity.HasOne(c => c.CreatedBy)
                  .WithMany()
                  .HasForeignKey(c => c.CreatedById)
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
        });
        // Bug Configuration
        modelBuilder.Entity<Bug>(entity =>
        {
            entity.HasKey(b => b.Id);
            // CreatedBy Relationship
            entity.HasOne(b => b.CreatedBy)
                  .WithMany()
                  .HasForeignKey("CreatedById")
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
            // AssignedTo Relationship  
            entity.HasOne(b => b.AssignedTo)
                  .WithMany()
                  .HasForeignKey("AssignedToId")
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
        });
        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
        });
        // StatusTransition Configuration
        modelBuilder.Entity<StatusTransition>(entity =>
        {
            entity.HasKey(st => st.Id);
            // Bug Relationship
            entity.HasOne(st => st.Bug)
                  .WithMany()
                  .HasForeignKey(st => st.BugId)
                  .OnDelete(DeleteBehavior.Cascade);
            // User Relationship (who made the change)
            entity.HasOne(st => st.ChangedBy)
                  .WithMany()
                  .HasForeignKey(st => st.ChangedById)
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
        });
    }
}