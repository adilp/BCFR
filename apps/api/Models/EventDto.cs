using System;
using System.Collections.Generic;

namespace MemberOrgApi.Models;

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventTime { get; set; } = string.Empty; // HH:mm format
    public string EndTime { get; set; } = string.Empty; // HH:mm format
    public string Location { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string? SpeakerTitle { get; set; }
    public string? SpeakerBio { get; set; }
    public DateTime RsvpDeadline { get; set; }
    public int? MaxAttendees { get; set; }
    public bool AllowPlusOne { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public RsvpStatsDto? RsvpStats { get; set; }
}

public class CreateEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventTime { get; set; } = string.Empty; // HH:mm format
    public string EndTime { get; set; } = string.Empty; // HH:mm format
    public string Location { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string? SpeakerTitle { get; set; }
    public string? SpeakerBio { get; set; }
    public DateTime RsvpDeadline { get; set; }
    public int? MaxAttendees { get; set; }
    public bool AllowPlusOne { get; set; } = true;
    public string Status { get; set; } = "draft";
}

public class UpdateEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string? SpeakerTitle { get; set; }
    public string? SpeakerBio { get; set; }
    public DateTime RsvpDeadline { get; set; }
    public int? MaxAttendees { get; set; }
    public bool AllowPlusOne { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RsvpStatsDto
{
    public int Yes { get; set; }
    public int No { get; set; }
    public int Pending { get; set; }
    public int PlusOnes { get; set; }
}

public class EventRsvpDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool HasPlusOne { get; set; }
    public DateTime ResponseDate { get; set; }
    public bool CheckedIn { get; set; }
    public DateTime? CheckInTime { get; set; }
}

public class CreateRsvpDto
{
    public string Response { get; set; } = "pending"; // yes, no, pending
    public bool HasPlusOne { get; set; } = false;
    public string? Notes { get; set; }
}

public class UpdateRsvpDto
{
    public string Response { get; set; } = string.Empty;
    public bool HasPlusOne { get; set; }
    public string? Notes { get; set; }
}