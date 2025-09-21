using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MemberOrgApi.Data;
using MemberOrgApi.Models;
using MemberOrgApi.Services;
using Npgsql;

namespace MemberOrgApi.Services;

public class EmailBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public EmailBackgroundService(IServiceProvider serviceProvider, ILogger<EmailBackgroundService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMs = _configuration.GetValue<int>("EmailQueue:ProcessingIntervalMs", 10_000);
        _logger.LogInformation("Email background service started. Interval: {Interval}ms", intervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await ProcessScheduledJobsAsync(db, scope.ServiceProvider, stoppingToken);
                await ProcessQueueAsync(db, emailService, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EmailBackgroundService loop");
            }

            try
            {
                await Task.Delay(intervalMs, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task ProcessScheduledJobsAsync(AppDbContext db, IServiceProvider scopedProvider, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        List<ScheduledEmailJob> dueJobs;
        try
        {
            dueJobs = await db.ScheduledEmailJobs
                .Where(j => j.Status == "Active" && (j.ScheduledFor <= now || (j.NextRunDate != null && j.NextRunDate <= now)))
                .OrderBy(j => j.ScheduledFor)
                .Take(20)
                .ToListAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Table doesn't exist yet (migrations pending). Skip this cycle.
            _logger.LogInformation("ScheduledEmailJobs table not found yet; skipping processing this cycle.");
            return;
        }

        if (dueJobs.Count == 0) return;

        foreach (var job in dueJobs)
        {
            try
            {
                switch (job.EntityType)
                {
                    case "Event":
                        // Resolve scoped dependencies
                        var tokenService = scopedProvider.GetRequiredService<ITokenService>();
                        var config = scopedProvider.GetRequiredService<IConfiguration>();
                        if (job.JobType == "EventAttendeeReminder")
                        {
                            await CreateEventAttendeeReminderEmailsFromJob(db, job, config, ct);
                        }
                        else if (job.JobType == "EventRsvpDeadlineReminder")
                        {
                            await CreateEventRsvpDeadlineReminderEmailsFromJob(db, job, tokenService, config, ct);
                        }
                        else
                        {
                            // Back-compat: treat unknown as non-RSVP reminder
                            await CreateEventRsvpDeadlineReminderEmailsFromJob(db, job, tokenService, config, ct);
                        }
                        break;
                    default:
                        _logger.LogWarning("Unknown ScheduledEmailJob EntityType={EntityType} Id={JobId}", job.EntityType, job.Id);
                        break;
                }

                job.LastRunDate = now;
                job.RunCount++;
                if (string.IsNullOrWhiteSpace(job.RecurrenceRule))
                {
                    job.Status = "Completed";
                }
                else
                {
                    job.NextRunDate = CalculateNextRun(job.RecurrenceRule!, now);
                }
                job.UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                job.FailureCount++;
                job.UpdatedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Failed processing ScheduledEmailJob {JobId}", job.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private DateTime? CalculateNextRun(string recurrenceRule, DateTime from)
    {
        return recurrenceRule switch
        {
            "DAILY" => from.AddDays(1),
            "WEEKLY" => from.AddDays(7),
            "MONTHLY" => from.AddMonths(1),
            _ => null
        };
    }

    // Two days before RSVP deadline: remind non-RSVP users with RSVP buttons
    private async Task CreateEventRsvpDeadlineReminderEmailsFromJob(AppDbContext db, ScheduledEmailJob job, ITokenService tokenService, IConfiguration config, CancellationToken ct)
    {
        if (!Guid.TryParse(job.EntityId, out var eventId))
        {
            _logger.LogWarning("Scheduled job {JobId} has invalid Event EntityId {EntityId}", job.Id, job.EntityId);
            return;
        }

        var evt = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (evt == null) return;

        // Build campaign
        var campaign = new EmailCampaign
        {
            Id = Guid.NewGuid(),
            Name = $"RSVP Deadline Reminder - {evt.Title}",
            Type = "EventRsvpDeadlineReminder",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        db.EmailCampaigns.Add(campaign);
        await db.SaveChangesAsync(ct);

        // Identify users who have not RSVPed
        var allUsers = await db.Users.Where(u => u.IsActive).ToListAsync(ct);
        var rsvpedUserIds = await db.EventRsvps.Where(r => r.EventId == evt.Id).Select(r => r.UserId).ToListAsync(ct);
        var nonRsvpUsers = allUsers.Where(u => !rsvpedUserIds.Contains(u.Id)).ToList();

        if (nonRsvpUsers.Count == 0) return;

        foreach (var user in nonRsvpUsers)
        {
            var token = await tokenService.GenerateRsvpTokenAsync(user.Id, evt.Id, evt.RsvpDeadline);
            var htmlBody = BuildDeadlineReminderHtml(config, evt, user.FirstName, token.Token);

            db.EmailQueue.Add(new EmailQueueItem
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                RecipientEmail = user.Email,
                RecipientName = $"{user.FirstName} {user.LastName}",
                Subject = $"RSVP Reminder: {evt.Title} — Respond by {TimeZoneInfo.ConvertTimeFromUtc(evt.RsvpDeadline, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago")).ToString("ddd, MMM d")}",
                HtmlBody = htmlBody,
                Status = "Pending",
                Priority = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        campaign.TotalRecipients = nonRsvpUsers.Count;
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Queued {Count} RSVP deadline reminders for event {EventId} via ScheduledJob {JobId}", nonRsvpUsers.Count, evt.Id, job.Id);
    }

    private static string BuildDeadlineReminderHtml(IConfiguration configuration, Event evt, string firstName, string rsvpToken)
    {
        var apiBase = configuration["App:ApiUrl"] ?? "http://localhost:5001/api";
        var yesUrl = $"{apiBase}/email-rsvp/respond?token={Uri.EscapeDataString(rsvpToken)}&response=yes";
        var noUrl = $"{apiBase}/email-rsvp/respond?token={Uri.EscapeDataString(rsvpToken)}&response=no";
        var yesWithGuestUrl = evt.AllowPlusOne
            ? $"{apiBase}/email-rsvp/respond?token={Uri.EscapeDataString(rsvpToken)}&response=yes&plusOne=true"
            : null;

        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateLocal = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, central);
        var dateStr = eventDateLocal.ToString("dddd, MMMM dd, yyyy");
        var deadlineLocal = TimeZoneInfo.ConvertTimeFromUtc(evt.RsvpDeadline, central);
        var deadlineStr = deadlineLocal.ToString("dddd, MMM d, h:mm tt") + " CT";
        var start = DateTime.Today.Add(evt.StartTime).ToString("h:mm tt");
        var end = DateTime.Today.Add(evt.EndTime).ToString("h:mm tt");

        return $@"<!DOCTYPE html>
        <html>
        <head><meta charset='utf-8' /><title>{evt.Title} - RSVP Deadline Reminder</title></head>
        <body style='font-family: -apple-system, BlinkMacSystemFont, Inter, Segoe UI, Roboto, sans-serif; background: #fdf8f1; padding: 24px; color: #212529;'>
          <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
            <div style='background:#6B3AA0;color:#fff;padding:20px'>
              <h1 style='margin:0;font-size:22px'>RSVP Deadline Approaching</h1>
            </div>
            <div style='padding:24px'>
              <p style='margin-top:0'>Hello {firstName},</p>
              <p>Please RSVP for <strong>{evt.Title}</strong>. The RSVP deadline is <strong>{deadlineStr}</strong>.</p>
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

    // One day before event: remind attendees who RSVPed YES (no RSVP buttons)
    private async Task CreateEventAttendeeReminderEmailsFromJob(AppDbContext db, ScheduledEmailJob job, IConfiguration config, CancellationToken ct)
    {
        if (!Guid.TryParse(job.EntityId, out var eventId)) return;
        var evt = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (evt == null) return;

        var yesRsvpUserIds = await db.EventRsvps
            .Where(r => r.EventId == evt.Id && r.Response == "yes")
            .Select(r => r.UserId)
            .ToListAsync(ct);

        if (yesRsvpUserIds.Count == 0) return;

        var users = await db.Users.Where(u => yesRsvpUserIds.Contains(u.Id)).ToListAsync(ct);

        var campaign = new EmailCampaign
        {
            Id = Guid.NewGuid(),
            Name = $"Attendee Reminder - {evt.Title}",
            Type = "EventAttendeeReminder",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        db.EmailCampaigns.Add(campaign);
        await db.SaveChangesAsync(ct);

        foreach (var user in users)
        {
            var htmlBody = BuildAttendeeReminderHtml(config, evt, user.FirstName);
            db.EmailQueue.Add(new EmailQueueItem
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                RecipientEmail = user.Email,
                RecipientName = $"{user.FirstName} {user.LastName}",
                Subject = $"Reminder: {evt.Title} is tomorrow",
                HtmlBody = htmlBody,
                Status = "Pending",
                Priority = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        campaign.TotalRecipients = users.Count;
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Queued {Count} attendee reminders for event {EventId} via ScheduledJob {JobId}", users.Count, evt.Id, job.Id);
    }

    private static string BuildAttendeeReminderHtml(IConfiguration configuration, Event evt, string firstName)
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
        <head><meta charset='utf-8' /><title>{evt.Title} - Attendee Reminder</title></head>
        <body style='font-family: -apple-system, BlinkMacSystemFont, Inter, Segoe UI, Roboto, sans-serif; background: #fdf8f1; padding: 24px; color: #212529;'>
          <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
            <div style='background:#6B3AA0;color:#fff;padding:20px'>
              <h1 style='margin:0;font-size:22px'>Event Reminder</h1>
            </div>
            <div style='padding:24px'>
              <p style='margin-top:0'>Hello {firstName},</p>
              <p>This is a reminder that you RSVP'd <strong>YES</strong> for <strong>{evt.Title}</strong>. We look forward to seeing you there!</p>
              <ul>
                <li><strong>Date:</strong> {dateStr}</li>
                <li><strong>Time:</strong> {start} – {end} CT</li>
                <li><strong>Location:</strong> {evt.Location}</li>
                <li><strong>Speaker:</strong> {evt.Speaker}</li>
              </ul>
              <div style='text-align:center;margin:12px 0'>
                <a href='{icsUrl}' style='background:#4263EB;color:#fff;padding:10px 16px;border-radius:8px;text-decoration:none;display:inline-block'>Add to Calendar (.ics)</a>
              </div>
              <p style='color:#6b7280;font-size:14px'>If your plans change, please update your RSVP from your account.</p>
            </div>
            <div style='background:#F5F2ED;color:#6b7280;padding:16px;text-align:center;font-size:12px'>
              Birmingham Committee on Foreign Relations
            </div>
          </div>
        </body>
        </html>";
    }
    private async Task ProcessQueueAsync(AppDbContext db, IEmailService emailService, CancellationToken ct)
    {
        var batchSize = _configuration.GetValue<int>("EmailQueue:BatchSize", 10);
        // Hard-coded delay between emails (no appsettings): 1000ms
        var delayMs = 1000;

        var now = DateTime.UtcNow;

        var dueItems = await db.EmailQueue
            .Where(e => (e.Status == "Pending" || e.Status == "Scheduled")
                        && (e.ScheduledFor == null || e.ScheduledFor <= now)
                        && (e.NextRetryAt == null || e.NextRetryAt <= now))
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (dueItems.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} email(s) from queue", dueItems.Count);

        foreach (var item in dueItems)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Mark as sending
                item.Status = "Sending";
                item.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                // Ensure plain text exists
                if (string.IsNullOrWhiteSpace(item.PlainTextBody))
                {
                    item.PlainTextBody = StripHtml(item.HtmlBody);
                }

                var ok = await emailService.SendCustomEmailAsync(item.RecipientEmail, item.Subject, item.HtmlBody, item.PlainTextBody);

                if (ok)
                {
                    item.Status = "Sent";
                    item.SentAt = DateTime.UtcNow;
                    item.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Sent email to {Recipient}", item.RecipientEmail);
                }
                else
                {
                    item.Status = "Failed";
                    item.FailedAt = DateTime.UtcNow;
                    item.ErrorMessage = "Provider returned false";
                    item.UpdatedAt = DateTime.UtcNow;
                    _logger.LogWarning("Email send reported failure for {Recipient}", item.RecipientEmail);
                }

                await db.SaveChangesAsync(ct);

                // rate limiting
                await Task.Delay(delayMs, ct);
            }
            catch (Exception ex)
            {
                try
                {
                    item.Status = "Failed";
                    item.FailedAt = DateTime.UtcNow;
                    item.ErrorMessage = ex.Message;
                    item.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to persist failure state for email {Id}", item.Id);
                }

                _logger.LogError(ex, "Error sending email {Id} to {Recipient}", item.Id, item.RecipientEmail);
            }
        }
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var text = Regex.Replace(html, "<[^>]+>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Trim();
    }
}
