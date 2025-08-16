using System;

namespace MemberOrgApi.Models;

public class EventRsvp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Response { get; set; } = "pending"; // yes, no, pending
    public bool HasPlusOne { get; set; } = false;
    public DateTime ResponseDate { get; set; } = DateTime.UtcNow;
    public bool CheckedIn { get; set; } = false;
    public DateTime? CheckInTime { get; set; }
    public string? Notes { get; set; }
    public DateTime? UpdatedAt { get; set; }
}