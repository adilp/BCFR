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
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventRsvp> EventRsvps { get; set; }
    public DbSet<RsvpToken> RsvpTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<EmailCampaign> EmailCampaigns { get; set; }
    public DbSet<EmailQueueItem> EmailQueue { get; set; }
    public DbSet<ScheduledEmailJob> ScheduledEmailJobs { get; set; }

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
            entity.Property(e => e.DietaryRestrictions).HasColumnType("jsonb");
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
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Auto-increment
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

        // Configure ActivityLog entity
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActivityCategory).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.OldValue).HasColumnType("jsonb");
            entity.Property(e => e.NewValue).HasColumnType("jsonb");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.PerformedBy)
                .WithMany()
                .HasForeignKey(e => e.PerformedById)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ActivityType);
            entity.HasIndex(e => e.ActivityCategory);
        });

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Speaker).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SpeakerTitle).HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20)
                .HasDefaultValue("draft");
            
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.EventDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RsvpDeadline);
        });

        // Configure EventRsvp entity
        modelBuilder.Entity<EventRsvp>(entity =>
        {
            entity.ToTable("EventRsvps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Response).IsRequired().HasMaxLength(20)
                .HasDefaultValue("pending");
            
            entity.HasOne(e => e.Event)
                .WithMany(e => e.Rsvps)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Ensure one RSVP per user per event
            entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.Response);
            entity.HasIndex(e => e.ResponseDate);
        });

        // Configure RsvpToken entity
        modelBuilder.Entity<RsvpToken>(entity =>
        {
            entity.ToTable("RsvpTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.UsedForResponse).HasMaxLength(20);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Event)
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure one token per user per event
            entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure EmailCampaign entity
        modelBuilder.Entity<EmailCampaign>(entity =>
        {
            entity.ToTable("EmailCampaigns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(e => e.CreatedBy).HasMaxLength(255);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_EmailCampaigns_CreatedAt");
        });

        // Configure EmailQueueItem entity
        modelBuilder.Entity<EmailQueueItem>(entity =>
        {
            entity.ToTable("EmailQueue");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RecipientName).HasMaxLength(255);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PlainTextBody).HasColumnType("text");
            entity.Property(e => e.HtmlBody).IsRequired().HasColumnType("text");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.Priority).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.ProviderMessageId).HasMaxLength(255);

            entity.HasOne(e => e.Campaign)
                .WithMany()
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.NoAction);

            // Unique dedupe per campaign
            entity.HasIndex(e => new { e.CampaignId, e.RecipientEmail })
                .IsUnique()
                .HasDatabaseName("UQ_Campaign_Recipient");

            // Processing index (partial equivalent via filtered index in Postgres)
            entity.HasIndex(e => new { e.Status, e.ScheduledFor, e.NextRetryAt })
                .HasDatabaseName("IX_EmailQueue_Processing");

            entity.HasIndex(e => e.CampaignId).HasDatabaseName("IX_EmailQueue_CampaignId");
            entity.HasIndex(e => e.UpdatedAt).HasDatabaseName("IX_EmailQueue_UpdatedAt");
        });

        // Configure ScheduledEmailJob entity
        modelBuilder.Entity<ScheduledEmailJob>(entity =>
        {
            entity.ToTable("ScheduledEmailJobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(e => e.RecurrenceRule).HasMaxLength(20);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.HasIndex(e => new { e.Status, e.ScheduledFor, e.NextRunDate }).HasDatabaseName("IX_ScheduledEmailJobs_Active");
            entity.HasIndex(e => new { e.EntityType, e.EntityId }).HasDatabaseName("IX_ScheduledEmailJobs_Entity");
        });

        // Configure PasswordResetToken entity
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.RequestedIp).HasMaxLength(45);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
