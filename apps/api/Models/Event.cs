using System;
using System.Collections.Generic;

namespace MemberOrgApi.Models;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string? SpeakerTitle { get; set; }
    public string? SpeakerBio { get; set; }
    public DateTime RsvpDeadline { get; set; }
    public int? MaxAttendees { get; set; }
    public bool AllowPlusOne { get; set; } = true;
    public string Status { get; set; } = "draft"; // draft, published, cancelled
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public ICollection<EventRsvp> Rsvps { get; set; } = new List<EventRsvp>();
}