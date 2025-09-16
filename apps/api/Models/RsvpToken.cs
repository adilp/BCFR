using System;

namespace MemberOrgApi.Models;

public class RsvpToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedForResponse { get; set; } // Track what response was submitted: yes, no
    public bool? UsedWithPlusOne { get; set; } // Track if plus one was included
}