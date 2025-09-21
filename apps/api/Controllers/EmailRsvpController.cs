using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MemberOrgApi.Data;
using MemberOrgApi.DTOs;
using MemberOrgApi.Models;
using MemberOrgApi.Services;
using MemberOrgApi.Constants;
using System.Text;

namespace MemberOrgApi.Controllers
{
    [ApiController]
    [Route("email-rsvp")]
    public class EmailRsvpController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IActivityLogService _activityLogService;
        private readonly IEmailQueueService _emailQueueService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailRsvpController> _logger;

        public EmailRsvpController(
            AppDbContext context,
            ITokenService tokenService,
            IActivityLogService activityLogService,
            IEmailQueueService emailQueueService,
            IConfiguration configuration,
            ILogger<EmailRsvpController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _activityLogService = activityLogService;
            _emailQueueService = emailQueueService;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: /email-rsvp/respond?token=xxx&response=yes&plusOne=false
        [HttpGet("respond")]
        public async Task<IActionResult> RespondViaEmail([FromQuery] string token, [FromQuery] string response, [FromQuery] bool? plusOne = null)
        {
            try
            {
                // Validate token
                var rsvpToken = await _tokenService.ValidateRsvpTokenAsync(token);
                if (rsvpToken == null)
                {
                    return Content(GenerateHtmlResponse(false, "Invalid or Expired Token",
                        "This RSVP link is invalid or has expired. Please log in to the website to RSVP."),
                        "text/html");
                }

                // Validate response
                if (response != "yes" && response != "no")
                {
                    return Content(GenerateHtmlResponse(false, "Invalid Response",
                        "Invalid RSVP response. Please use the buttons in the email."),
                        "text/html");
                }

                // Get event details
                var evt = await _context.Events.FindAsync(rsvpToken.EventId);
                if (evt == null)
                {
                    return Content(GenerateHtmlResponse(false, "Event Not Found",
                        "The event associated with this RSVP no longer exists."),
                        "text/html");
                }

                // Check if RSVP deadline has passed
                if (DateTime.UtcNow > evt.RsvpDeadline)
                {
                    return Content(GenerateHtmlResponse(false, "RSVP Deadline Passed",
                        $"Sorry, the RSVP deadline for \"{evt.Title}\" has passed."),
                        "text/html");
                }

                // Check if event is full (only for "yes" responses)
                if (response == "yes" && evt.MaxAttendees.HasValue)
                {
                    var currentAttendees = await _context.EventRsvps
                        .Where(r => r.EventId == rsvpToken.EventId && r.Response == "yes")
                        .CountAsync();

                    if (currentAttendees >= evt.MaxAttendees.Value)
                    {
                        return Content(GenerateHtmlResponse(false, "Event Full",
                            $"Sorry, \"{evt.Title}\" is full and cannot accept more attendees."),
                            "text/html");
                    }
                }

                // Check if plus one is allowed
                var hasPlusOne = response == "yes" && plusOne == true && evt.AllowPlusOne;

                // Check for existing RSVP
                var existingRsvp = await _context.EventRsvps
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.EventId == rsvpToken.EventId && r.UserId == rsvpToken.UserId);

                string previousResponse = null;
                bool isNewRsvp = false;

                if (existingRsvp != null)
                {
                    // Update existing RSVP
                    previousResponse = existingRsvp.Response;
                    existingRsvp.Response = response;
                    existingRsvp.HasPlusOne = hasPlusOne;
                    existingRsvp.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new RSVP
                    isNewRsvp = true;
                    existingRsvp = new EventRsvp
                    {
                        EventId = rsvpToken.EventId,
                        UserId = rsvpToken.UserId,
                        Response = response,
                        HasPlusOne = hasPlusOne,
                        ResponseDate = DateTime.UtcNow
                    };
                    _context.EventRsvps.Add(existingRsvp);
                }

                // Mark token as used
                await _tokenService.MarkTokenAsUsedAsync(token, response, hasPlusOne);

                // Save changes
                await _context.SaveChangesAsync();

                // Log the activity
                var activityType = response == "yes"
                    ? ActivityTypes.EventRegistration
                    : ActivityTypes.EventCancellation;

                var description = isNewRsvp
                    ? $"RSVP'd '{response}' to event: {evt.Title} (via email)"
                    : $"Changed RSVP from '{previousResponse}' to '{response}' for event: {evt.Title} (via email)";

                var metadata = new Dictionary<string, object>
                {
                    { "EventId", evt.Id },
                    { "EventTitle", evt.Title },
                    { "EventDate", evt.EventDate },
                    { "Response", response },
                    { "HasPlusOne", hasPlusOne },
                    { "ViaEmail", true }
                };

                if (!isNewRsvp && previousResponse != null)
                {
                    metadata["PreviousResponse"] = previousResponse;
                }

                await _activityLogService.LogActivityAsync(
                    rsvpToken.UserId,
                    activityType,
                    description,
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    metadata: metadata
                );

                // Generate success message
                var user = rsvpToken.User ?? await _context.Users.FindAsync(rsvpToken.UserId);
                var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Member";

                string successMessage = response == "yes"
                    ? $"Thank you, {userName}! You have successfully RSVP'd YES to \"{evt.Title}\"."
                    : $"Thank you, {userName}. We've recorded that you cannot attend \"{evt.Title}\".";

                if (hasPlusOne)
                {
                    successMessage += " We've also noted that you'll be bringing a guest.";
                }

                // If RSVP is YES (new or changed to yes), queue confirmation email with ICS
                if (response == "yes" && (isNewRsvp || previousResponse != "yes"))
                {
                    try
                    {
                        user = rsvpToken.User ?? await _context.Users.FindAsync(rsvpToken.UserId);
                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            var campaign = await EnsureRsvpConfirmationCampaign(evt);
                            var html = BuildRsvpConfirmationHtml(_configuration, evt, user.FirstName);
                            await _emailQueueService.QueueSingleEmailAsync(
                                to: user.Email,
                                subject: $"RSVP Confirmed: {evt.Title}",
                                htmlBody: html,
                                recipientName: $"{user.FirstName} {user.LastName}",
                                campaignId: campaign.Id
                            );
                        }
                    }
                    catch (Exception exq)
                    {
                        _logger.LogWarning(exq, "Failed to queue RSVP confirmation email for event {EventId} user {UserId}", evt.Id, rsvpToken.UserId);
                    }
                }

                return Content(GenerateHtmlResponse(true, "RSVP Successful", successMessage, evt), "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing email RSVP with token: {token}");
                return Content(GenerateHtmlResponse(false, "Error",
                    "An error occurred while processing your RSVP. Please try again or contact support."),
                    "text/html");
            }
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

        private static string BuildRsvpConfirmationHtml(IConfiguration configuration, Event evt, string firstName)
        {
            var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
            var eventDateLocal = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, central);
            var dateStr = eventDateLocal.ToString("dddd, MMMM dd, yyyy");
            var start = DateTime.Today.Add(evt.StartTime).ToString("h:mm tt");
            var end = DateTime.Today.Add(evt.EndTime).ToString("h:mm tt");
            var apiBase = configuration["App:ApiUrl"] ?? "http://localhost:5001/api";
            var icsUrl = $"{apiBase}/events/{evt.Id}/calendar.ics";

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

        private string GenerateHtmlResponse(bool success, string title, string message, Event? evt = null)
        {
            // Get the frontend URL from configuration
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var frontendUrl = configuration["App:BaseUrl"] ?? "http://localhost:5173";

            var statusColor = success ? "#22C55E" : "#EF4444";
            var statusIcon = success ? "✓" : "✗";

            var eventDetailsHtml = "";
            if (evt != null && success)
            {
                var eventDate = evt.EventDate.ToString("dddd, MMMM dd, yyyy");
                var startTime = DateTime.Today.Add(evt.StartTime).ToString("h:mm tt");
                var endTime = DateTime.Today.Add(evt.EndTime).ToString("h:mm tt");

                eventDetailsHtml = $@"
                    <div style='background: #F5F2ED; border-radius: 8px; padding: 24px; margin: 24px 0;'>
                        <h3 style='color: #212529; margin: 0 0 16px 0; font-size: 20px;'>Event Details</h3>
                        <p style='margin: 8px 0; color: #495057;'><strong>Date:</strong> {eventDate}</p>
                        <p style='margin: 8px 0; color: #495057;'><strong>Time:</strong> {startTime} - {endTime}</p>
                        <p style='margin: 8px 0; color: #495057;'><strong>Location:</strong> {evt.Location}</p>
                        <p style='margin: 8px 0; color: #495057;'><strong>Speaker:</strong> {evt.Speaker}</p>
                    </div>";
            }

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>{title} - BCFR</title>
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
                        line-height: 1.6;
                        color: #212529;
                        margin: 0;
                        padding: 0;
                        background-color: #fdf8f1;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 20px;
                        background-color: #ffffff;
                        border-radius: 16px;
                        overflow: hidden;
                        box-shadow: 0 6px 20px rgba(0, 0, 0, 0.08);
                    }}
                    .header {{
                        background: {(success ? "linear-gradient(135deg, #22C55E 0%, #16A34A 100%)" : "linear-gradient(135deg, #EF4444 0%, #DC2626 100%)")};
                        color: white;
                        padding: 48px 40px;
                        text-align: center;
                    }}
                    .status-icon {{
                        font-size: 64px;
                        margin: 0 0 16px 0;
                    }}
                    .header h1 {{
                        margin: 0;
                        font-size: 32px;
                        font-weight: 700;
                        letter-spacing: -0.02em;
                    }}
                    .content {{
                        padding: 40px;
                    }}
                    .message {{
                        font-size: 18px;
                        color: #495057;
                        margin: 0 0 32px 0;
                        text-align: center;
                    }}
                    .button {{
                        display: inline-block;
                        padding: 14px 32px;
                        background: #4263EB;
                        color: white;
                        text-decoration: none;
                        border-radius: 8px;
                        font-weight: 600;
                        font-size: 16px;
                        margin: 24px 0;
                    }}
                    .button:hover {{
                        background: #3B5BDB;
                    }}
                    .footer {{
                        background-color: #F5F2ED;
                        padding: 32px;
                        text-align: center;
                    }}
                    .footer p {{
                        color: #6C757D;
                        font-size: 14px;
                        margin: 0 0 8px 0;
                    }}
                    .footer a {{
                        color: #4263EB;
                        text-decoration: none;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <div class='status-icon'>{statusIcon}</div>
                        <h1>{title}</h1>
                    </div>
                    <div class='content'>
                        <p class='message'>{message}</p>
                        {eventDetailsHtml}
                        <div style='text-align: center;'>
                            <a href='{frontendUrl}/events' class='button'>View All Events</a>
                        </div>
                    </div>
                    <div class='footer'>
                        <p><strong>Birmingham Committee on Foreign Relations</strong></p>
                        <p>© 2025 BCFR. All rights reserved.</p>
                        <p><a href='{frontendUrl}'>Visit Our Website</a></p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
