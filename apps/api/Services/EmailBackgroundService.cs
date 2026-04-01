using System.Text.RegularExpressions;
using System.Text;
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

        // Ensure monthly billing report job exists
        await EnsureMonthlyBillingReportJobAsync(stoppingToken);

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

    private async Task EnsureMonthlyBillingReportJobAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var exists = await db.ScheduledEmailJobs
                .AnyAsync(j => j.JobType == "MonthlyBillingReport" && j.EntityType == "Subscription" && j.Status == "Active", ct);

            if (!exists)
            {
                // Schedule for the 1st of next month at 9am Central
                var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
                var nowCentral = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, central);
                var firstOfNextMonth = new DateTime(nowCentral.Year, nowCentral.Month, 1, 9, 0, 0).AddMonths(1);
                var scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(firstOfNextMonth, DateTimeKind.Unspecified), central);

                db.ScheduledEmailJobs.Add(new ScheduledEmailJob
                {
                    Id = Guid.NewGuid(),
                    JobType = "MonthlyBillingReport",
                    EntityType = "Subscription",
                    EntityId = "system",
                    ScheduledFor = scheduledUtc,
                    RecurrenceRule = "MONTHLY",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Created monthly billing report scheduled job for {Date}", firstOfNextMonth);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not seed monthly billing report job (will retry next startup)");
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
                    case "Subscription":
                        if (job.JobType == "MonthlyBillingReport")
                        {
                            await CreateMonthlyBillingReportAsync(db, ct);
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
                    var nextRun = CalculateNextRun(job.RecurrenceRule!, job.NextRunDate ?? job.ScheduledFor);
                    job.NextRunDate = nextRun;
                    job.ScheduledFor = nextRun ?? job.ScheduledFor;
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
              <p style='margin:0 0 4px 0'>Birmingham Committee on Foreign Relations</p>
              <p style='margin:0 0 4px 0'>2001 Park Pl, Suite 450, Birmingham, Alabama 35203, US</p>
              <p style='margin:8px 0 0 0;font-size:11px;color:#9CA3AF'><a href='{apiBase}/unsubscribe' style='color:#9CA3AF'>Unsubscribe</a> from these emails</p>
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
              <p style='margin:0 0 4px 0'>Birmingham Committee on Foreign Relations</p>
              <p style='margin:0 0 4px 0'>2001 Park Pl, Suite 450, Birmingham, Alabama 35203, US</p>
              <p style='margin:8px 0 0 0;font-size:11px;color:#9CA3AF'><a href='{apiBase}/unsubscribe' style='color:#9CA3AF'>Unsubscribe</a> from these emails</p>
            </div>
          </div>
        </body>
        </html>";
    }
    private async Task CreateMonthlyBillingReportAsync(AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var thirtyDaysOut = now.AddDays(30);

        // 1. Users with no active subscription (no record at all, or cancelled/expired)
        var usersWithActiveSub = await db.MembershipSubscriptions
            .Where(s => s.Status == "active")
            .Select(s => s.UserId)
            .ToListAsync(ct);

        var usersWithoutSub = await db.Users
            .Where(u => u.IsActive && !usersWithActiveSub.Contains(u.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        // 2. Upcoming Stripe renewals (next 30 days, real Stripe customers)
        var upcomingStripe = await db.MembershipSubscriptions
            .Include(s => s.User)
            .Where(s => s.Status == "active"
                && !s.StripeCustomerId.StartsWith("CHECK_")
                && s.NextBillingDate >= now && s.NextBillingDate <= thirtyDaysOut)
            .OrderBy(s => s.NextBillingDate)
            .ToListAsync(ct);

        // 3. Upcoming check renewals (next 30 days)
        var upcomingCheck = await db.MembershipSubscriptions
            .Include(s => s.User)
            .Where(s => s.Status == "active"
                && s.StripeCustomerId.StartsWith("CHECK_")
                && s.NextBillingDate >= now && s.NextBillingDate <= thirtyDaysOut)
            .OrderBy(s => s.NextBillingDate)
            .ToListAsync(ct);

        // Build HTML sections
        string BuildBillingRow(MembershipSubscription s)
        {
            var billingDate = TimeZoneInfo.ConvertTimeFromUtc(s.NextBillingDate, central);
            return $@"<tr>
                <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{s.User.FirstName} {s.User.LastName}</td>
                <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{s.User.Email}</td>
                <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{s.MembershipTier}</td>
                <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>${s.Amount:F2}</td>
                <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{billingDate:MMM d, yyyy}</td>
            </tr>";
        }

        string BuildBillingTable(string title, string color, List<MembershipSubscription> items, string emptyMessage)
        {
            var rows = items.Count > 0
                ? string.Join("\n", items.Select(BuildBillingRow))
                : $"<tr><td colspan='5' style='padding:12px;text-align:center;color:#6b7280'>{emptyMessage}</td></tr>";

            return $@"
            <div style='margin-bottom:24px'>
                <h2 style='font-size:16px;color:{color};margin:0 0 8px 0'>{title} ({items.Count})</h2>
                <table style='width:100%;border-collapse:collapse;font-size:14px'>
                    <thead>
                        <tr style='background:#f9fafb'>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Name</th>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Email</th>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Tier</th>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Amount</th>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Renewal Date</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>
            </div>";
        }

        // No-subscription section uses a simpler table (no tier/amount/date)
        var noSubRows = usersWithoutSub.Count > 0
            ? string.Join("\n", usersWithoutSub.Select(u =>
                $@"<tr>
                    <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{u.FirstName} {u.LastName}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{u.Email}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{u.Phone ?? "-"}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #e5e7eb'>{TimeZoneInfo.ConvertTimeFromUtc(u.CreatedAt, central):MMM d, yyyy}</td>
                </tr>"))
            : "<tr><td colspan='4' style='padding:12px;text-align:center;color:#6b7280'>All members have an active subscription</td></tr>";

        var noSubSection = $@"
            <div style='margin-bottom:24px'>
                <h2 style='font-size:16px;color:#dc2626;margin:0 0 8px 0'>No Active Subscription ({usersWithoutSub.Count})</h2>
                <p style='font-size:13px;color:#6b7280;margin:0 0 8px 0'>Members without a current payment on record</p>
                <table style='width:100%;border-collapse:collapse;font-size:14px'>
                    <thead>
                        <tr style='background:#f9fafb'>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Name</th>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Email</th>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Phone</th>
                            <th style='padding:8px 12px;text-align:left;border-bottom:2px solid #e5e7eb'>Joined</th>
                        </tr>
                    </thead>
                    <tbody>{noSubRows}</tbody>
                </table>
            </div>";

        var stripeSection = BuildBillingTable("Upcoming Stripe Renewals (Next 30 Days)", "#4263EB", upcomingStripe, "No upcoming Stripe renewals");
        var checkSection = BuildBillingTable("Upcoming Check Renewals (Next 30 Days)", "#1976d2", upcomingCheck, "No upcoming check renewals");

        var reportDate = TimeZoneInfo.ConvertTimeFromUtc(now, central);
        var htmlBody = $@"<!DOCTYPE html>
        <html>
        <head><meta charset='utf-8' /><title>Monthly Billing Report</title></head>
        <body style='font-family: -apple-system, BlinkMacSystemFont, Inter, Segoe UI, Roboto, sans-serif; background: #fdf8f1; padding: 24px; color: #212529;'>
          <div style='max-width:700px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
            <div style='background:#6B3AA0;color:#fff;padding:20px'>
              <h1 style='margin:0;font-size:22px'>Monthly Billing Report</h1>
              <p style='margin:4px 0 0 0;opacity:0.85;font-size:14px'>{reportDate:MMMM d, yyyy}</p>
            </div>
            <div style='padding:24px'>
              {noSubSection}
              {stripeSection}
              {checkSection}
            </div>
            <div style='background:#F5F2ED;color:#6b7280;padding:16px;text-align:center;font-size:12px'>
              <p style='margin:0'>Birmingham Committee on Foreign Relations — Admin Report</p>
            </div>
          </div>
        </body>
        </html>";

        // Send to all admins
        var admins = await db.Users.Where(u => u.Role == "Admin" && u.IsActive).ToListAsync(ct);
        if (admins.Count == 0)
        {
            _logger.LogWarning("No active admins found for monthly billing report");
            return;
        }

        var campaign = new EmailCampaign
        {
            Id = Guid.NewGuid(),
            Name = $"Monthly Billing Report - {reportDate:MMMM yyyy}",
            Type = "BillingReport",
            Status = "Active",
            TotalRecipients = admins.Count,
            CreatedAt = DateTime.UtcNow
        };
        db.EmailCampaigns.Add(campaign);

        foreach (var admin in admins)
        {
            db.EmailQueue.Add(new EmailQueueItem
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                RecipientEmail = admin.Email,
                RecipientName = $"{admin.FirstName} {admin.LastName}",
                Subject = $"BCFR Billing Report — {reportDate:MMMM yyyy}",
                HtmlBody = htmlBody,
                Status = "Pending",
                Priority = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Queued monthly billing report to {Count} admin(s). No subscription: {NoSub}, Stripe upcoming: {Stripe}, Check upcoming: {Check}",
            admins.Count, usersWithoutSub.Count, upcomingStripe.Count, upcomingCheck.Count);
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

                // Attach ICS only for final attendee reminders
                List<EmailAttachment>? attachments = null;
                if (item.CampaignId != null)
                {
                    var campaign = await db.EmailCampaigns.FirstOrDefaultAsync(c => c.Id == item.CampaignId.Value, ct);
                    if (campaign?.Type == "EventAttendeeReminder" || campaign?.Type == "EventRsvpConfirmation")
                    {
                        var maybeEventId = TryExtractEventIdFromIcsLink(item.HtmlBody);
                        if (maybeEventId != null)
                        {
                            var evt = await db.Events.FirstOrDefaultAsync(e => e.Id == maybeEventId.Value, ct);
                            if (evt != null)
                            {
                                var icsContent = BuildIcsForEvent(evt);
                                attachments = new List<EmailAttachment>
                                {
                                    new EmailAttachment
                                    {
                                        FileName = $"event-{evt.Id}.ics",
                                        ContentType = "text/calendar; charset=UTF-8; method=PUBLISH",
                                        Base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(icsContent))
                                    }
                                };
                            }
                        }
                    }
                }

                var ok = await emailService.SendCustomEmailAsync(item.RecipientEmail, item.Subject, item.HtmlBody, item.PlainTextBody, attachments);

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

    private static Guid? TryExtractEventIdFromIcsLink(string html)
    {
        if (string.IsNullOrEmpty(html)) return null;
        // Looks for /events/{GUID}/calendar.ics
        var match = Regex.Match(html, @"/events/([0-9a-fA-F\-]{36})/calendar\.ics");
        if (match.Success && Guid.TryParse(match.Groups[1].Value, out var id))
        {
            return id;
        }
        return null;
    }

    private static string BuildIcsForEvent(Event evt)
    {
        // Compute start/end in UTC based on Central time date + times
        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var eventDateLocalDate = TimeZoneInfo.ConvertTimeFromUtc(evt.EventDate, central).Date;
        var startLocal = eventDateLocalDate.Add(evt.StartTime);
        var endLocal = eventDateLocalDate.Add(evt.EndTime);
        if (endLocal <= startLocal)
        {
            endLocal = startLocal.AddHours(1);
        }
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), central);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), central);

        static string FormatIcsDate(DateTime dt) => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

        static string Fold(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            const int limit = 75;
            if (line.Length <= limit) return line;
            var sb = new System.Text.StringBuilder();
            int idx = 0;
            while (idx < line.Length)
            {
                int take = Math.Min(limit, line.Length - idx);
                var chunk = line.Substring(idx, take);
                if (idx > 0) sb.Append("\r\n ");
                sb.Append(chunk);
                idx += take;
            }
            return sb.ToString();
        }

        var prodId = "-//BCFR//Events//EN";
        var organizerEmail = "no-reply@birminghamforeignrelations.org"; // fallback static organizer

        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            $"PRODID:{prodId}",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{evt.Id}@birminghamforeignrelations.org",
            $"DTSTAMP:{FormatIcsDate(DateTime.UtcNow)}",
            $"DTSTART:{FormatIcsDate(startUtc)}",
            $"DTEND:{FormatIcsDate(endUtc)}",
            Fold($"SUMMARY:{EscapeIcsText(evt.Title)}"),
            Fold($"DESCRIPTION:{EscapeIcsText(evt.Description)}"),
            Fold($"LOCATION:{EscapeIcsText(evt.Location)}"),
            $"SEQUENCE:0",
            $"ORGANIZER:MAILTO:{organizerEmail}",
            "END:VEVENT",
            "END:VCALENDAR"
        };

        return string.Join("\r\n", lines) + "\r\n";
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
}
