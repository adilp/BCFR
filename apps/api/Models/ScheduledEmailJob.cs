using System.Text.Json.Nodes;

namespace MemberOrgApi.Models;

public class ScheduledEmailJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JobType { get; set; } = string.Empty; // EventFirstReminder, EventFinalReminder, etc.
    public string EntityType { get; set; } = string.Empty; // Event, Subscription, Newsletter
    public string EntityId { get; set; } = string.Empty;   // Guid string for Event
    public DateTime ScheduledFor { get; set; }
    public string? RecurrenceRule { get; set; } // e.g., MONTHLY, WEEKLY
    public DateTime? NextRunDate { get; set; }
    public DateTime? LastRunDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Completed, Cancelled
    public int RunCount { get; set; } = 0;
    public int FailureCount { get; set; } = 0;
    public JsonObject? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

