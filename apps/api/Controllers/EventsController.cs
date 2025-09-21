using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
    private readonly IEmailService _emailService;
    private readonly IEmailQueueService _emailQueue;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;

    public EventsController(
        AppDbContext context,
        ILogger<EventsController> logger,
        IActivityLogService activityLogService,
        IEmailService emailService,
        IEmailQueueService emailQueueService,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _activityLogService = activityLogService;
        _emailService = emailService;
        _emailQueue = emailQueueService;
        _tokenService = tokenService;
        _configuration = configuration;
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

    // GET: /events/{id}/calendar.ics - Download ICS for event
    [HttpGet("{id}/calendar.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventCalendar(Guid id)
    {
        var evt = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (evt == null)
        {
            return NotFound();
        }

        // Compute start/end in UTC based on Central time date + times
        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateLocalDate = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, central).Date;
        var startLocal = eventDateLocalDate.Add(evt.StartTime);
        var endLocal = eventDateLocalDate.Add(evt.EndTime);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), central);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), central);

        string FormatIcsDate(DateTime dt) => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

        var ics = $@"BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//BCFR//Events//EN\r\nCALSCALE:GREGORIAN\r\nMETHOD:PUBLISH\r\nBEGIN:VEVENT\r\nUID:{evt.Id}@birminghamforeignrelations.org\r\nDTSTAMP:{FormatIcsDate(DateTime.UtcNow)}\r\nDTSTART:{FormatIcsDate(startUtc)}\r\nDTEND:{FormatIcsDate(endUtc)}\r\nSUMMARY:{EscapeIcsText(evt.Title)}\r\nDESCRIPTION:{EscapeIcsText(evt.Description)}\r\nLOCATION:{EscapeIcsText(evt.Location)}\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n";

        Response.Headers["Content-Disposition"] = $"attachment; filename=event-{evt.Id}.ics";
        return Content(ics, "text/calendar");
    }

    private static string EscapeIcsText(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\n", "\\n");
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

        // Queue announcement campaign if event is published
        if (evt.Status == "published")
        {
            await QueueEventAnnouncementCampaign(evt);
            await ScheduleEventReminderJobs(evt);
        }

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

        var previousStatus = evt.Status;

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

        // Queue announcement campaign if event is being published for the first time
        if (previousStatus != "published" && updateDto.Status == "published")
        {
            await QueueEventAnnouncementCampaign(evt);
            await ScheduleEventReminderJobs(evt);
        }

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

        string? previousResponse = null;
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

        // Queue RSVP confirmation email if this is a YES and it's new or changed to yes
        if (createDto.Response == "yes" && (isNewRsvp || previousResponse != "yes"))
        {
            try
            {
                var campaign = await EnsureRsvpConfirmationCampaign(evt);
                var html = BuildRsvpConfirmationHtml(evt, existingRsvp.User.FirstName);
                await _emailQueue.QueueSingleEmailAsync(
                    to: existingRsvp.User.Email,
                    subject: $"RSVP Confirmed: {evt.Title}",
                    htmlBody: html,
                    recipientName: $"{existingRsvp.User.FirstName} {existingRsvp.User.LastName}",
                    campaignId: campaign.Id
                );
            }
            catch (Exception exq)
            {
                _logger.LogWarning(exq, "Failed to queue RSVP confirmation email for event {EventId} user {UserId}", evt.Id, existingRsvp.UserId);
            }
        }

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

    // POST: /events/{id}/remind-non-rsvps - Send reminder emails to users who haven't RSVPed (admin only)
    [HttpPost("{id}/remind-non-rsvps")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendReminderToNonRsvpUsers(Guid id)
    {
        try
        {
            var evt = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (evt == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims for remind-non-rsvps request");
                return Unauthorized(new { message = "User authentication failed" });
            }

            // Get all active users
            var allUsers = await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            // Get users who have already RSVPed to this event
            var rsvpedUserIds = await _context.EventRsvps
                .Where(r => r.EventId == id)
                .Select(r => r.UserId)
                .ToListAsync();

            // Filter to get users who haven't RSVPed
            var nonRsvpUsers = allUsers
                .Where(u => !rsvpedUserIds.Contains(u.Id))
                .ToList();

            if (!nonRsvpUsers.Any())
            {
                return Ok(new { message = "All active users have already RSVPed to this event", count = 0 });
            }

            // Store the count before passing to background task
            var userCount = nonRsvpUsers.Count;
            var eventTitle = evt.Title;

            // Create a campaign grouping for this reminder batch
            var campaign = new EmailCampaign
            {
                Name = $"Event Reminder - {evt.Title}",
                Type = "EventReminder",
                Status = "Active",
                TotalRecipients = nonRsvpUsers.Count,
                CreatedAt = DateTime.UtcNow
            };
            _context.EmailCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            // Queue emails linked to the campaign
            var successCount = 0;
            foreach (var user in nonRsvpUsers)
            {
                // Generate RSVP token per user
                if (!IsValidEmail(user.Email)) continue;
                try
                {
                    var token = await _tokenService.GenerateRsvpTokenAsync(user.Id, evt.Id, evt.RsvpDeadline);
                    var htmlBody = BuildEventEmailHtml(evt, user.FirstName, token.Token);
                    // Queue per user to embed unique token
                    await _emailQueue.QueueSingleEmailAsync(user.Email,
                        subject: $"Reminder: {evt.Title} is coming up!",
                        htmlBody: htmlBody,
                        recipientName: $"{user.FirstName} {user.LastName}",
                        campaignId: campaign.Id);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to queue reminder email for user {UserId} {Email}", user.Id, user.Email);
                }
            }
            campaign.TotalRecipients = successCount;

            await _activityLogService.LogActivityAsync(
                Guid.Parse(userId),
                ActivityTypes.EventReminderSent,
                ActivityCategories.Communication,
                $"Sent reminder emails for event '{eventTitle}' to {userCount} non-RSVP users"
            );

            return Ok(new { message = $"Queued reminder emails to {userCount} users who haven't RSVPed", count = userCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendReminderToNonRsvpUsers for event {EventId}", id);
            return StatusCode(500, new { message = "An error occurred while sending reminder emails" });
        }
    }

    private async Task SendEventAnnouncementEmails(Event evt)
    {
        try
        {
            _logger.LogInformation("Starting to send announcement emails for event: {EventId}", evt.Id);

            // Get all active users (Members and Admins)
            var users = await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            if (!users.Any())
            {
                _logger.LogWarning("No active users found to send event announcements");
                return;
            }

            var successCount = 0;
            var failedEmails = new List<string>();

            // Send emails to each user with their unique RSVP token
            foreach (var user in users)
            {
                try
                {
                    // Generate unique RSVP token for this user and event
                    var rsvpToken = await _tokenService.GenerateRsvpTokenAsync(user.Id, evt.Id, evt.RsvpDeadline);

                    // Send the announcement email
                    var emailSent = await _emailService.SendEventAnnouncementEmailAsync(
                        user.Email,
                        user.FirstName,
                        evt.Title,
                        evt.Description,
                        evt.EventDate,
                        evt.StartTime,
                        evt.EndTime,
                        evt.Location,
                        evt.Speaker,
                        evt.RsvpDeadline,
                        evt.AllowPlusOne,
                        rsvpToken.Token
                    );

                    if (emailSent)
                    {
                        successCount++;
                        _logger.LogInformation("Event announcement sent to {Email} for event {EventId}", user.Email, evt.Id);
                    }
                    else
                    {
                        failedEmails.Add(user.Email);
                        _logger.LogWarning("Failed to send event announcement to {Email} for event {EventId}", user.Email, evt.Id);
                    }

                    // Add delay to respect Resend's rate limit (2 requests per second)
                    // Using 600ms to stay safely under the limit
                    await Task.Delay(600);
                }
                catch (Exception ex)
                {
                    failedEmails.Add(user.Email);
                    _logger.LogError(ex, "Error sending event announcement to {Email} for event {EventId}", user.Email, evt.Id);
                }
            }

            _logger.LogInformation("Event announcement emails sent: {SuccessCount} successful, {FailedCount} failed for event {EventId}",
                successCount, failedEmails.Count, evt.Id);

            if (failedEmails.Any())
            {
                _logger.LogWarning("Failed to send announcements to: {FailedEmails}", string.Join(", ", failedEmails));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event announcement emails for event {EventId}", evt.Id);
        }
    }

    private async Task SendEventAnnouncementEmailsScoped(Event evt, AppDbContext scopedContext, IEmailService scopedEmailService, ITokenService scopedTokenService, ILogger<EventsController> scopedLogger)
    {
        try
        {
            scopedLogger.LogInformation("Starting to send announcement emails for event: {EventId}", evt.Id);

            // Get all active users (Members and Admins)
            var users = await scopedContext.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            if (!users.Any())
            {
                scopedLogger.LogWarning("No active users found to send event announcements");
                return;
            }

            var successCount = 0;
            var failedEmails = new List<string>();

            // Send emails to each user with their unique RSVP token
            foreach (var user in users)
            {
                try
                {
                    // Generate unique RSVP token for this user and event
                    var rsvpToken = await scopedTokenService.GenerateRsvpTokenAsync(user.Id, evt.Id, evt.RsvpDeadline);

                    // Send the announcement email
                    var emailSent = await scopedEmailService.SendEventAnnouncementEmailAsync(
                        user.Email,
                        user.FirstName,
                        evt.Title,
                        evt.Description,
                        evt.EventDate,
                        evt.StartTime,
                        evt.EndTime,
                        evt.Location,
                        evt.Speaker,
                        evt.RsvpDeadline,
                        evt.AllowPlusOne,
                        rsvpToken.Token
                    );

                    if (emailSent)
                    {
                        successCount++;
                        scopedLogger.LogInformation("Event announcement sent to {Email} for event {EventId}", user.Email, evt.Id);
                    }
                    else
                    {
                        failedEmails.Add(user.Email);
                        scopedLogger.LogWarning("Failed to send event announcement to {Email} for event {EventId}", user.Email, evt.Id);
                    }

                    // Add delay to respect Resend's rate limit (2 requests per second)
                    // Using 600ms to stay safely under the limit
                    await Task.Delay(600);
                }
                catch (Exception ex)
                {
                    failedEmails.Add(user.Email);
                    scopedLogger.LogError(ex, "Error sending event announcement to {Email} for event {EventId}", user.Email, evt.Id);
                }
            }

            scopedLogger.LogInformation("Event announcement emails sent: {SuccessCount} successful, {FailedCount} failed for event {EventId}",
                successCount, failedEmails.Count, evt.Id);

            if (failedEmails.Any())
            {
                scopedLogger.LogWarning("Failed to send announcements to: {FailedEmails}", string.Join(", ", failedEmails));
            }
        }
        catch (Exception ex)
        {
            scopedLogger.LogError(ex, "Error sending event announcement emails for event {EventId}", evt.Id);
        }
    }

    private async Task SendEventReminderEmailsScoped(Event evt, List<User> users, IEmailService scopedEmailService, ITokenService scopedTokenService, ILogger<EventsController> scopedLogger)
    {
        try
        {
            scopedLogger.LogInformation("Starting to send reminder emails for event {EventId} to {UserCount} non-RSVP users",
                evt.Id, users.Count);

            var successCount = 0;
            var failedEmails = new List<string>();

            // Send reminder emails to each user with their unique RSVP token
            foreach (var user in users)
            {
                try
                {
                    // Generate unique RSVP token for this user and event
                    var rsvpToken = await scopedTokenService.GenerateRsvpTokenAsync(user.Id, evt.Id, evt.RsvpDeadline);

                    // Send the reminder email (reusing the announcement email template)
                    var emailSent = await scopedEmailService.SendEventAnnouncementEmailAsync(
                        user.Email,
                        user.FirstName,
                        evt.Title,
                        evt.Description,
                        evt.EventDate,
                        evt.StartTime,
                        evt.EndTime,
                        evt.Location,
                        evt.Speaker,
                        evt.RsvpDeadline,
                        evt.AllowPlusOne,
                        rsvpToken.Token
                    );

                    if (emailSent)
                    {
                        successCount++;
                        scopedLogger.LogInformation("Reminder email sent to {Email} for event {EventId}", user.Email, evt.Id);
                    }
                    else
                    {
                        failedEmails.Add(user.Email);
                        scopedLogger.LogWarning("Failed to send reminder email to {Email} for event {EventId}", user.Email, evt.Id);
                    }

                    // Add delay to respect Resend's rate limit (2 requests per second)
                    // Using 600ms to stay safely under the limit
                    await Task.Delay(600);
                }
                catch (Exception ex)
                {
                    failedEmails.Add(user.Email);
                    scopedLogger.LogError(ex, "Error sending reminder email to {Email} for event {EventId}", user.Email, evt.Id);
                }
            }

            scopedLogger.LogInformation("Event reminder emails sent: {SuccessCount} successful, {FailedCount} failed for event {EventId}",
                successCount, failedEmails.Count, evt.Id);

            if (failedEmails.Any())
            {
                scopedLogger.LogWarning("Failed to send reminders to: {FailedEmails}", string.Join(", ", failedEmails));
            }
        }
        catch (Exception ex)
        {
            scopedLogger.LogError(ex, "Error sending event reminder emails for event {EventId}", evt.Id);
        }
    }

    private async Task QueueEventAnnouncementCampaign(Event evt)
    {
        // Build campaign
        var campaign = new EmailCampaign
        {
            Name = $"Event Announcement - {evt.Title}",
            Type = "EventAnnouncement",
            Status = "Active",
            TotalRecipients = 0, // will update after queuing
            CreatedAt = DateTime.UtcNow
        };
        _context.EmailCampaigns.Add(campaign);
        await _context.SaveChangesAsync();

        // Get all active users
        var users = await _context.Users
            .Where(u => u.IsActive)
            .ToListAsync();

        var count = 0;
        foreach (var user in users)
        {
            if (!IsValidEmail(user.Email)) continue;
            try
            {
                // RSVP token per user
                var token = await _tokenService.GenerateRsvpTokenAsync(user.Id, evt.Id, evt.RsvpDeadline);
                var htmlBody = BuildEventEmailHtml(evt, user.FirstName, token.Token, header: "You're Invited: Event Announcement");

                await _emailQueue.QueueSingleEmailAsync(
                    to: user.Email,
                    subject: $"You're invited: {evt.Title}",
                    htmlBody: htmlBody,
                    recipientName: $"{user.FirstName} {user.LastName}",
                    campaignId: campaign.Id
                );
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue announcement email for user {UserId} {Email}", user.Id, user.Email);
            }
        }

        // Update campaign recipient count
        campaign.TotalRecipients = count;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Queued announcement campaign {CampaignId} for event {EventId} with {Count} recipients", campaign.Id, evt.Id, count);
    }

    private bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task ScheduleEventReminderJobs(Event evt)
    {
        // Schedule two jobs:
        // 1) 2 days before RSVP deadline (non-RSVP users, with RSVP buttons)
        // 2) 1 day before event (attendee reminder to YES RSVPs, no RSVP buttons)

        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateCentral = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, central).Date;
        var rsvpDeadlineCentral = TimeZoneInfo.ConvertTimeFromUtc(evt.RsvpDeadline, central).Date;

        DateTime ToUtcCentral(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), central);

        // 2 days before RSVP deadline at 9:00 AM CT
        var deadlineReminderLocal = rsvpDeadlineCentral.AddDays(-2).AddHours(9);
        var deadlineWhenUtc = ToUtcCentral(deadlineReminderLocal);
        var deadlineScheduledFor = deadlineWhenUtc < DateTime.UtcNow ? DateTime.UtcNow.AddMinutes(1) : deadlineWhenUtc;

        var existsDeadline = await _context.ScheduledEmailJobs.AnyAsync(j =>
            j.JobType == "EventRsvpDeadlineReminder" &&
            j.EntityType == "Event" &&
            j.EntityId == evt.Id.ToString() &&
            j.Status == "Active");
        if (!existsDeadline)
        {
            _context.ScheduledEmailJobs.Add(new ScheduledEmailJob
            {
                JobType = "EventRsvpDeadlineReminder",
                EntityType = "Event",
                EntityId = evt.Id.ToString(),
                ScheduledFor = deadlineScheduledFor,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // 1 day before event at 9:00 AM CT
        var attendeeReminderLocal = eventDateCentral.AddDays(-1).AddHours(9);
        var attendeeWhenUtc = ToUtcCentral(attendeeReminderLocal);
        var attendeeScheduledFor = attendeeWhenUtc < DateTime.UtcNow ? DateTime.UtcNow.AddMinutes(1) : attendeeWhenUtc;

        var existsAttendee = await _context.ScheduledEmailJobs.AnyAsync(j =>
            j.JobType == "EventAttendeeReminder" &&
            j.EntityType == "Event" &&
            j.EntityId == evt.Id.ToString() &&
            j.Status == "Active");
        if (!existsAttendee)
        {
            _context.ScheduledEmailJobs.Add(new ScheduledEmailJob
            {
                JobType = "EventAttendeeReminder",
                EntityType = "Event",
                EntityId = evt.Id.ToString(),
                ScheduledFor = attendeeScheduledFor,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Scheduled RSVP deadline and attendee reminder jobs for event {EventId}", evt.Id);
    }

    private string BuildEventEmailHtml(Event evt, string firstName, string rsvpToken, string header = "Upcoming Event Reminder")
    {
        var apiBase = _configuration["App:ApiUrl"] ?? "http://localhost:5001/api";
        var yesUrl = $"{apiBase}/email-rsvp/respond?token={Uri.EscapeDataString(rsvpToken)}&response=yes";
        var noUrl = $"{apiBase}/email-rsvp/respond?token={Uri.EscapeDataString(rsvpToken)}&response=no";
        var yesWithGuestUrl = evt.AllowPlusOne
            ? $"{apiBase}/email-rsvp/respond?token={Uri.EscapeDataString(rsvpToken)}&response=yes&plusOne=true"
            : null;
        // No .ics link here; attachment and link are only in final attendee reminder

        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateLocal = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, central);
        var dateStr = eventDateLocal.ToString("dddd, MMMM dd, yyyy");
        var start = DateTime.Today.Add(evt.StartTime).ToString("h:mm tt");
        var end = DateTime.Today.Add(evt.EndTime).ToString("h:mm tt");

        return $@"<!DOCTYPE html>
        <html>
        <head><meta charset='utf-8' /><title>{evt.Title} - Reminder</title></head>
        <body style='font-family: -apple-system, BlinkMacSystemFont, Inter, Segoe UI, Roboto, sans-serif; background: #fdf8f1; padding: 24px; color: #212529;'>
          <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
            <div style='background:#6B3AA0;color:#fff;padding:20px'>
              <h1 style='margin:0;font-size:22px'>{header}</h1>
            </div>
            <div style='padding:24px'>
              <p style='margin-top:0'>Hello {firstName},</p>
              <p>Reminder for <strong>{evt.Title}</strong>.</p>
              <ul>
                <li><strong>Date:</strong> {dateStr}</li>
                <li><strong>Time:</strong> {start} – {end} CT</li>
                <li><strong>Location:</strong> {evt.Location}</li>
                <li><strong>Speaker:</strong> {evt.Speaker}</li>
                {(evt.AllowPlusOne ? "<li><strong>Guests:</strong> Plus-one allowed</li>" : "")}
              </ul>
              <p>Please RSVP below:</p>
              <div style='text-align:center;margin:24px 0'>
                <a href='{yesUrl}' style='background:#22c55e;color:#fff;padding:12px 18px;border-radius:8px;text-decoration:none;margin-right:12px;display:inline-block'>RSVP Yes</a>
                {(yesWithGuestUrl != null ? $"<a href='{yesWithGuestUrl}' style='background:#16a34a;color:#fff;padding:12px 18px;border-radius:8px;text-decoration:none;margin-right:12px;display:inline-block'>RSVP Yes + Guest</a>" : "")}
                <a href='{noUrl}' style='background:#ef4444;color:#fff;padding:12px 18px;border-radius:8px;text-decoration:none;display:inline-block'>RSVP No</a>
              </div>
              
              <p style='color:#6b7280;font-size:14px'>If the buttons don't work, copy and paste these links into your browser:<br/>
                Yes: {yesUrl}<br/>
                {(yesWithGuestUrl != null ? $"Yes + Guest: {yesWithGuestUrl}<br/>" : "")}
                No: {noUrl}
                
              </p>
            </div>
            <div style='background:#F5F2ED;color:#6b7280;padding:16px;text-align:center;font-size:12px'>
              Birmingham Committee on Foreign Relations
            </div>
          </div>
        </body>
        </html>";
    }

    private async Task<EmailCampaign> EnsureRsvpConfirmationCampaign(Event evt)
    {
        var name = $"RSVP Confirmation - {evt.Id}";
        var existing = await _context.EmailCampaigns.FirstOrDefaultAsync(c => c.Name == name && c.Type == "EventRsvpConfirmation");
        if (existing != null) return existing;

        var campaign = new EmailCampaign
        {
            Name = name,
            Type = "EventRsvpConfirmation",
            Status = "Active",
            TotalRecipients = 0,
            CreatedAt = DateTime.UtcNow
        };
        _context.EmailCampaigns.Add(campaign);
        await _context.SaveChangesAsync();
        return campaign;
    }

    private string BuildRsvpConfirmationHtml(Event evt, string firstName)
    {
        var apiBase = _configuration["App:ApiUrl"] ?? "http://localhost:5001/api";
        var icsUrl = $"{apiBase}/events/{evt.Id}/calendar.ics";
        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateLocal = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, central);
        var dateStr = eventDateLocal.ToString("dddd, MMMM dd, yyyy");
        var start = DateTime.Today.Add(evt.StartTime).ToString("h:mm tt");
        var end = DateTime.Today.Add(evt.EndTime).ToString("h:mm tt");

        return $@"<!DOCTYPE html>
        <html>
        <head><meta charset='utf-8' /><title>{evt.Title} - RSVP Confirmation</title></head>
        <body style='font-family: -apple-system, BlinkMacSystemFont, Inter, Segoe UI, Roboto, sans-serif; background: #fdf8f1; padding: 24px; color: #212529;'>
          <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
            <div style='background:#6B3AA0;color:#fff;padding:20px'>
              <h1 style='margin:0;font-size:22px'>RSVP Confirmed</h1>
            </div>
            <div style='padding:24px'>
              <p style='margin-top:0'>Hello {firstName},</p>
              <p>Thanks for RSVP'ing <strong>YES</strong> to <strong>{evt.Title}</strong>.</p>
              <ul>
                <li><strong>Date:</strong> {dateStr}</li>
                <li><strong>Time:</strong> {start} – {end} CT</li>
                <li><strong>Location:</strong> {evt.Location}</li>
                <li><strong>Speaker:</strong> {evt.Speaker}</li>
              </ul>
              <div style='text-align:center;margin:12px 0'>
                <a href='{icsUrl}' style='background:#4263EB;color:#fff;padding:10px 16px;border-radius:8px;text-decoration:none;display:inline-block'>Add to Calendar (.ics)</a>
              </div>
              <p style='color:#6b7280;font-size:14px'>Calendar: {icsUrl}</p>
            </div>
            <div style='background:#F5F2ED;color:#6b7280;padding:16px;text-align:center;font-size:12px'>
              Birmingham Committee on Foreign Relations
            </div>
          </div>
        </body>
        </html>";
    }
}
