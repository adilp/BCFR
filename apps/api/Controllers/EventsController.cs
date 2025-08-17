using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;
using MemberOrgApi.Models;
using MemberOrgApi.DTOs;
using MemberOrgApi.Services;
using MemberOrgApi.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemberOrgApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<EventsController> _logger;
    private readonly IActivityLogService _activityLogService;

    public EventsController(AppDbContext context, ILogger<EventsController> logger, IActivityLogService activityLogService)
    {
        _context = context;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    // GET: /events - Get all published events (public)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents([FromQuery] string? status = null)
    {
        var query = _context.Events.AsQueryable();
        
        // If not authenticated or not admin, only show published events
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            query = query.Where(e => e.Status == "published");
        }
        else if (!User.IsInRole("Admin") && status == null)
        {
            query = query.Where(e => e.Status == "published");
        }
        else if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(e => e.Status == status);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartTime)
            .ToListAsync();

        var eventDtos = new List<EventDto>();
        foreach (var evt in events)
        {
            var dto = MapToEventDto(evt);
            dto.RsvpStats = await GetRsvpStats(evt.Id);
            eventDtos.Add(dto);
        }

        return Ok(eventDtos);
    }

    // GET: /events/{id} - Get single event
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(Guid id)
    {
        var evt = await _context.Events
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null)
        {
            return NotFound(new { message = "Event not found" });
        }

        // Check if user can view draft/cancelled events
        if (evt.Status != "published" && (!User.Identity?.IsAuthenticated ?? true))
        {
            return NotFound(new { message = "Event not found" });
        }

        var dto = MapToEventDto(evt);
        dto.RsvpStats = await GetRsvpStats(evt.Id);

        return Ok(dto);
    }

    // POST: /events - Create new event (Admin only)
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<EventDto>> CreateEvent(CreateEventDto createDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Parse time strings to TimeSpan
        if (!TryParseTime(createDto.EventTime, out var startTime))
        {
            return BadRequest(new { message = "Invalid start time format. Use HH:mm" });
        }
        if (!TryParseTime(createDto.EndTime, out var endTime))
        {
            return BadRequest(new { message = "Invalid end time format. Use HH:mm" });
        }

        // Validate RSVP deadline is before event date
        if (createDto.RsvpDeadline >= createDto.EventDate)
        {
            return BadRequest(new { message = "RSVP deadline must be before event date" });
        }

        // Convert Central Time dates to UTC (always use Central Time for Birmingham events)
        var centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateCentral = DateTime.SpecifyKind(createDto.EventDate, DateTimeKind.Unspecified);
        var rsvpDeadlineCentral = DateTime.SpecifyKind(createDto.RsvpDeadline, DateTimeKind.Unspecified);
        
        var eventDateUtc = TimeZoneInfo.ConvertTimeToUtc(eventDateCentral, centralTimeZone);
        var rsvpDeadlineUtc = TimeZoneInfo.ConvertTimeToUtc(rsvpDeadlineCentral, centralTimeZone);

        var evt = new Event
        {
            Title = createDto.Title,
            Description = createDto.Description,
            EventDate = eventDateUtc,
            StartTime = startTime,
            EndTime = endTime,
            Location = createDto.Location,
            Speaker = createDto.Speaker,
            SpeakerTitle = createDto.SpeakerTitle,
            SpeakerBio = createDto.SpeakerBio,
            RsvpDeadline = rsvpDeadlineUtc,
            MaxAttendees = createDto.MaxAttendees,
            AllowPlusOne = createDto.AllowPlusOne,
            Status = createDto.Status,
            CreatedById = Guid.Parse(userId),
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var dto = MapToEventDto(evt);
        dto.RsvpStats = new RsvpStatsDto { Yes = 0, No = 0, Pending = 0, PlusOnes = 0 };

        _logger.LogInformation("Event created: {EventId} by {UserId}", evt.Id, userId);

        return CreatedAtAction(nameof(GetEvent), new { id = evt.Id }, dto);
    }

    // PUT: /events/{id} - Update event (Admin only)
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateEvent(Guid id, UpdateEventDto updateDto)
    {
        var evt = await _context.Events.FindAsync(id);
        if (evt == null)
        {
            return NotFound(new { message = "Event not found" });
        }

        // Parse time strings to TimeSpan
        if (!TryParseTime(updateDto.EventTime, out var startTime))
        {
            return BadRequest(new { message = "Invalid start time format. Use HH:mm" });
        }
        if (!TryParseTime(updateDto.EndTime, out var endTime))
        {
            return BadRequest(new { message = "Invalid end time format. Use HH:mm" });
        }

        // Validate RSVP deadline is before event date
        if (updateDto.RsvpDeadline >= updateDto.EventDate)
        {
            return BadRequest(new { message = "RSVP deadline must be before event date" });
        }

        // Convert Central Time dates to UTC (always use Central Time for Birmingham events)
        var centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateCentral = DateTime.SpecifyKind(updateDto.EventDate, DateTimeKind.Unspecified);
        var rsvpDeadlineCentral = DateTime.SpecifyKind(updateDto.RsvpDeadline, DateTimeKind.Unspecified);
        
        var eventDateUtc = TimeZoneInfo.ConvertTimeToUtc(eventDateCentral, centralTimeZone);
        var rsvpDeadlineUtc = TimeZoneInfo.ConvertTimeToUtc(rsvpDeadlineCentral, centralTimeZone);

        evt.Title = updateDto.Title;
        evt.Description = updateDto.Description;
        evt.EventDate = eventDateUtc;
        evt.StartTime = startTime;
        evt.EndTime = endTime;
        evt.Location = updateDto.Location;
        evt.Speaker = updateDto.Speaker;
        evt.SpeakerTitle = updateDto.SpeakerTitle;
        evt.SpeakerBio = updateDto.SpeakerBio;
        evt.RsvpDeadline = rsvpDeadlineUtc;
        evt.MaxAttendees = updateDto.MaxAttendees;
        evt.AllowPlusOne = updateDto.AllowPlusOne;
        evt.Status = updateDto.Status;
        evt.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Event updated: {EventId}", evt.Id);

        return NoContent();
    }

    // DELETE: /events/{id} - Delete event (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var evt = await _context.Events.FindAsync(id);
        if (evt == null)
        {
            return NotFound(new { message = "Event not found" });
        }

        _context.Events.Remove(evt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event deleted: {EventId}", evt.Id);

        return NoContent();
    }

    // GET: /events/{id}/rsvps - Get RSVPs for an event (Admin only)
    [HttpGet("{id}/rsvps")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<EventRsvpDto>>> GetEventRsvps(Guid id)
    {
        var rsvps = await _context.EventRsvps
            .Include(r => r.User)
            .Where(r => r.EventId == id)
            .OrderBy(r => r.ResponseDate)
            .ToListAsync();

        var rsvpDtos = rsvps.Select(r => new EventRsvpDto
        {
            Id = r.Id,
            UserId = r.UserId,
            UserName = $"{r.User.FirstName} {r.User.LastName}",
            UserEmail = r.User.Email,
            Response = r.Response,
            HasPlusOne = r.HasPlusOne,
            ResponseDate = r.ResponseDate,
            CheckedIn = r.CheckedIn,
            CheckInTime = r.CheckInTime
        });

        return Ok(rsvpDtos);
    }

    // GET: /events/{id}/my-rsvp - Get current user's RSVP for an event
    [HttpGet("{id}/my-rsvp")]
    [Authorize]
    public async Task<ActionResult<EventRsvpDto>> GetMyRsvp(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var rsvp = await _context.EventRsvps
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == Guid.Parse(userId));

        if (rsvp == null)
        {
            return NotFound(new { message = "RSVP not found" });
        }

        var dto = new EventRsvpDto
        {
            Id = rsvp.Id,
            UserId = rsvp.UserId,
            UserName = $"{rsvp.User.FirstName} {rsvp.User.LastName}",
            UserEmail = rsvp.User.Email,
            Response = rsvp.Response,
            HasPlusOne = rsvp.HasPlusOne,
            ResponseDate = rsvp.ResponseDate,
            CheckedIn = rsvp.CheckedIn,
            CheckInTime = rsvp.CheckInTime
        };

        return Ok(dto);
    }

    // POST: /events/{id}/rsvp - Create or update RSVP for current user
    [HttpPost("{id}/rsvp")]
    [Authorize]
    public async Task<ActionResult<EventRsvpDto>> CreateOrUpdateRsvp(Guid id, CreateRsvpDto createDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var evt = await _context.Events.FindAsync(id);
        if (evt == null)
        {
            return NotFound(new { message = "Event not found" });
        }

        // Check if RSVP deadline has passed
        if (DateTime.UtcNow > evt.RsvpDeadline)
        {
            return BadRequest(new { message = "RSVP deadline has passed" });
        }

        // Check if event is full (only for "yes" responses)
        if (createDto.Response == "yes" && evt.MaxAttendees.HasValue)
        {
            var currentAttendees = await _context.EventRsvps
                .Where(r => r.EventId == id && r.Response == "yes")
                .CountAsync();
            
            if (currentAttendees >= evt.MaxAttendees.Value)
            {
                return BadRequest(new { message = "Event is full" });
            }
        }

        // Check if plus one is allowed
        if (createDto.HasPlusOne && !evt.AllowPlusOne)
        {
            return BadRequest(new { message = "Plus ones are not allowed for this event" });
        }

        var userGuid = Guid.Parse(userId);
        var existingRsvp = await _context.EventRsvps
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userGuid);

        string previousResponse = null;
        bool isNewRsvp = false;
        
        if (existingRsvp != null)
        {
            // Store previous response for logging
            previousResponse = existingRsvp.Response;
            
            // Update existing RSVP
            existingRsvp.Response = createDto.Response;
            existingRsvp.HasPlusOne = createDto.Response == "yes" ? createDto.HasPlusOne : false;
            existingRsvp.Notes = createDto.Notes;
            existingRsvp.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new RSVP
            isNewRsvp = true;
            existingRsvp = new EventRsvp
            {
                EventId = id,
                UserId = userGuid,
                Response = createDto.Response,
                HasPlusOne = createDto.Response == "yes" ? createDto.HasPlusOne : false,
                Notes = createDto.Notes,
                ResponseDate = DateTime.UtcNow
            };
            _context.EventRsvps.Add(existingRsvp);
            await _context.SaveChangesAsync();
            
            // Reload with User data
            existingRsvp = await _context.EventRsvps
                .Include(r => r.User)
                .FirstAsync(r => r.Id == existingRsvp.Id);
        }

        await _context.SaveChangesAsync();
        
        // Log the activity
        var activityType = createDto.Response == "yes" 
            ? ActivityTypes.EventRegistration 
            : ActivityTypes.EventCancellation;
        
        var description = isNewRsvp
            ? $"RSVP'd '{createDto.Response}' to event: {evt.Title}"
            : $"Changed RSVP from '{previousResponse}' to '{createDto.Response}' for event: {evt.Title}";
            
        var metadata = new Dictionary<string, object>
        {
            { "EventId", evt.Id },
            { "EventTitle", evt.Title },
            { "EventDate", evt.EventDate },
            { "Response", createDto.Response },
            { "HasPlusOne", createDto.HasPlusOne }
        };
        
        if (!isNewRsvp && previousResponse != null)
        {
            metadata.Add("PreviousResponse", previousResponse);
        }
        
        await _activityLogService.LogActivityAsync(
            userGuid,
            activityType,
            ActivityCategories.Engagement,
            description,
            oldValue: isNewRsvp ? null : previousResponse,
            newValue: createDto.Response,
            metadata: metadata
        );

        var dto = new EventRsvpDto
        {
            Id = existingRsvp.Id,
            UserId = existingRsvp.UserId,
            UserName = $"{existingRsvp.User.FirstName} {existingRsvp.User.LastName}",
            UserEmail = existingRsvp.User.Email,
            Response = existingRsvp.Response,
            HasPlusOne = existingRsvp.HasPlusOne,
            ResponseDate = existingRsvp.ResponseDate,
            CheckedIn = existingRsvp.CheckedIn,
            CheckInTime = existingRsvp.CheckInTime
        };

        _logger.LogInformation("RSVP created/updated: EventId={EventId}, UserId={UserId}, Response={Response}", 
            id, userId, createDto.Response);

        return Ok(dto);
    }

    // POST: /events/{id}/checkin/{userId} - Check in attendee (Admin only)
    [HttpPost("{id}/checkin/{userId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CheckInAttendee(Guid id, Guid userId)
    {
        var rsvp = await _context.EventRsvps
            .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);

        if (rsvp == null)
        {
            return NotFound(new { message = "RSVP not found" });
        }

        if (rsvp.Response != "yes")
        {
            return BadRequest(new { message = "Can only check in attendees who RSVP'd yes" });
        }

        rsvp.CheckedIn = true;
        rsvp.CheckInTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Get event details for logging
        var evt = await _context.Events.FindAsync(id);
        
        // Log the check-in activity
        await _activityLogService.LogActivityAsync(
            userId,
            ActivityTypes.EventAttendance,
            ActivityCategories.Engagement,
            $"Checked in to event: {evt?.Title}",
            metadata: new Dictionary<string, object>
            {
                { "EventId", id },
                { "EventTitle", evt?.Title ?? "Unknown" },
                { "CheckInTime", DateTime.UtcNow }
            }
        );

        _logger.LogInformation("Attendee checked in: EventId={EventId}, UserId={UserId}", id, userId);

        return NoContent();
    }

    // Helper methods
    private EventDto MapToEventDto(Event evt)
    {
        // Convert UTC dates back to Central Time for display (always use Central Time for Birmingham events)
        var centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateCentral = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, centralTimeZone);
        var rsvpDeadlineCentral = TimeZoneInfo.ConvertTimeFromUtc(evt.RsvpDeadline, centralTimeZone);
        
        return new EventDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            EventDate = eventDateCentral,
            EventTime = evt.StartTime.ToString(@"hh\:mm"),
            EndTime = evt.EndTime.ToString(@"hh\:mm"),
            Location = evt.Location,
            Speaker = evt.Speaker,
            SpeakerTitle = evt.SpeakerTitle,
            SpeakerBio = evt.SpeakerBio,
            RsvpDeadline = rsvpDeadlineCentral,
            MaxAttendees = evt.MaxAttendees,
            AllowPlusOne = evt.AllowPlusOne,
            Status = evt.Status,
            CreatedBy = evt.CreatedBy != null ? $"{evt.CreatedBy.FirstName} {evt.CreatedBy.LastName}" : null,
            CreatedAt = evt.CreatedAt,
            UpdatedAt = evt.UpdatedAt
        };
    }

    private async Task<RsvpStatsDto> GetRsvpStats(Guid eventId)
    {
        var rsvps = await _context.EventRsvps
            .Where(r => r.EventId == eventId)
            .ToListAsync();

        return new RsvpStatsDto
        {
            Yes = rsvps.Count(r => r.Response == "yes"),
            No = rsvps.Count(r => r.Response == "no"),
            Pending = rsvps.Count(r => r.Response == "pending"),
            PlusOnes = rsvps.Count(r => r.HasPlusOne)
        };
    }

    private bool TryParseTime(string timeStr, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrEmpty(timeStr))
            return false;

        var parts = timeStr.Split(':');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var hours) || !int.TryParse(parts[1], out var minutes))
            return false;

        if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
            return false;

        time = new TimeSpan(hours, minutes, 0);
        return true;
    }
}