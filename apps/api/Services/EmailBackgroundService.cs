using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MemberOrgApi.Data;
using MemberOrgApi.Models;
using MemberOrgApi.Services;

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
