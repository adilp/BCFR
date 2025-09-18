using System.Text.Json.Nodes;

namespace MemberOrgApi.Models;

public class EmailCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., EventAnnouncement, EventReminder
    public string Status { get; set; } = "Active"; // Active, Completed, Cancelled
    public int TotalRecipients { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? CreatedBy { get; set; }
    public JsonObject? Metadata { get; set; }
}

