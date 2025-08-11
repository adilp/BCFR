using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Models;

namespace MemberOrgApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<MembershipSubscription> MembershipSubscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use a custom schema to avoid public schema permission issues
        modelBuilder.HasDefaultSchema("memberorg");

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users"); // Explicitly set table name to match production DB
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });

        // Configure Session entity
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions"); // Explicitly set table name to match production DB
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure MembershipSubscription entity
        modelBuilder.Entity<MembershipSubscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MembershipTier).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StripeCustomerId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StripeSubscriptionId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}