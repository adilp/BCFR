using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MemberOrgApi.Data;
using MemberOrgApi.Models;

namespace MemberOrgApi.Services
{
    public class EmailJobProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailJobProcessor> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Check for jobs every 30 seconds
        private readonly TimeSpan _emailDelay = TimeSpan.FromSeconds(2); // 2 seconds between emails to respect rate limit

        public EmailJobProcessor(IServiceProvider serviceProvider, ILogger<EmailJobProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Job Processor started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingJobs(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email jobs");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Email Job Processor stopped");
        }

        private async Task ProcessPendingJobs(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var jobService = scope.ServiceProvider.GetRequiredService<IEmailJobService>();

            // Get next pending job (not scheduled for future)
            var job = await context.EmailJobs
                .Include(j => j.Recipients)
                .Where(j => j.Status == EmailJobStatus.Pending)
                .Where(j => j.ScheduledFor == null || j.ScheduledFor <= DateTime.UtcNow)
                .OrderBy(j => j.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (job == null)
            {
                return; // No jobs to process
            }

            // Check quota before processing
            var pendingCount = job.Recipients.Count(r => r.Status == EmailJobStatus.Pending);
            if (!await jobService.CanSendEmailsAsync(pendingCount))
            {
                _logger.LogWarning($"Job {job.Id} postponed due to quota limit");
                return; // Will retry next cycle
            }

            _logger.LogInformation($"Processing email job {job.Id} with {pendingCount} pending recipients");

            // Update job status
            job.Status = EmailJobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            // Process each pending recipient
            var successCount = 0;
            var failedCount = 0;

            foreach (var recipient in job.Recipients.Where(r => r.Status == EmailJobStatus.Pending))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Check if job was paused or cancelled
                await context.Entry(job).ReloadAsync(cancellationToken);
                if (job.Status == EmailJobStatus.Paused || job.Status == EmailJobStatus.Cancelled)
                {
                    _logger.LogInformation($"Job {job.Id} was {job.Status}, stopping processing");
                    return;
                }

                try
                {
                    // Send the email
                    var sent = await emailService.SendCustomEmailAsync(
                        recipient.Email,
                        job.Subject,
                        job.IsHtml ? job.Body : null,
                        job.IsHtml ? null : job.Body
                    );

                    if (sent)
                    {
                        recipient.Status = EmailJobStatus.Completed;
                        recipient.ProcessedAt = DateTime.UtcNow;
                        successCount++;

                        // Update quota
                        await IncrementQuotaUsage(context, 1);
                    }
                    else
                    {
                        recipient.Status = EmailJobStatus.Failed;
                        recipient.ErrorMessage = "Failed to send email";
                        recipient.ProcessedAt = DateTime.UtcNow;
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email to {recipient.Email}");
                    recipient.Status = EmailJobStatus.Failed;
                    recipient.ErrorMessage = ex.Message;
                    recipient.ProcessedAt = DateTime.UtcNow;
                    failedCount++;
                }

                // Update job progress
                job.ProcessedCount++;
                job.SuccessCount += successCount > 0 ? 1 : 0;
                job.FailedCount += failedCount > 0 ? 1 : 0;
                job.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Reset counters
                successCount = 0;
                failedCount = 0;

                // Delay between emails to respect rate limit
                if (recipient != job.Recipients.Last(r => r.Status == EmailJobStatus.Pending))
                {
                    await Task.Delay(_emailDelay, cancellationToken);
                }
            }

            // Update job as completed
            job.Status = job.FailedCount == job.TotalRecipients ? EmailJobStatus.Failed : EmailJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Completed job {job.Id}: {job.SuccessCount} sent, {job.FailedCount} failed");
        }

        private async Task IncrementQuotaUsage(AppDbContext context, int count)
        {
            var today = DateTime.UtcNow.Date;
            var quota = await context.EmailQuotas.FirstOrDefaultAsync(q => q.Date == today);
            
            if (quota == null)
            {
                quota = new EmailQuota
                {
                    Date = today,
                    EmailsSent = count,
                    QuotaLimit = 100
                };
                context.EmailQuotas.Add(quota);
            }
            else
            {
                quota.EmailsSent += count;
                quota.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
    }
}