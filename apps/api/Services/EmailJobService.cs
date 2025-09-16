using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MemberOrgApi.Data;
using MemberOrgApi.Models;

namespace MemberOrgApi.Services
{
    public interface IEmailJobService
    {
        Task<EmailJob> CreateJobAsync(Guid userId, string subject, string body, bool isHtml, List<string> recipients, DateTime? scheduledFor = null);
        Task<EmailJob> GetJobAsync(Guid jobId);
        Task<List<EmailJob>> GetJobsAsync(Guid? userId = null, string? status = null);
        Task<bool> CancelJobAsync(Guid jobId);
        Task<bool> PauseJobAsync(Guid jobId);
        Task<bool> ResumeJobAsync(Guid jobId);
        Task<EmailJobStats> GetJobStatsAsync(Guid? userId = null);
        Task<int> GetRemainingQuotaAsync();
        Task<bool> CanSendEmailsAsync(int count);
    }

    public class EmailJobService : IEmailJobService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmailJobService> _logger;
        private const int DAILY_QUOTA = 100; // Resend free tier limit

        public EmailJobService(AppDbContext context, ILogger<EmailJobService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailJob> CreateJobAsync(Guid userId, string subject, string body, bool isHtml, List<string> recipients, DateTime? scheduledFor = null)
        {
            // Check if we have quota
            var remainingQuota = await GetRemainingQuotaAsync();
            if (remainingQuota < recipients.Count)
            {
                throw new InvalidOperationException($"Insufficient email quota. You can send {remainingQuota} more emails today. This job requires {recipients.Count} emails.");
            }

            var job = new EmailJob
            {
                Id = Guid.NewGuid(),
                CreatedBy = userId,
                Subject = subject,
                Body = body,
                IsHtml = isHtml,
                TotalRecipients = recipients.Count,
                Status = scheduledFor.HasValue ? EmailJobStatus.Pending : EmailJobStatus.Pending,
                ScheduledFor = scheduledFor,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Recipients = new List<EmailJobRecipient>()
            };

            // Add recipients
            foreach (var email in recipients)
            {
                job.Recipients.Add(new EmailJobRecipient
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    Email = email,
                    Status = EmailJobStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.EmailJobs.Add(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created email job {job.Id} with {recipients.Count} recipients");
            return job;
        }

        public async Task<EmailJob> GetJobAsync(Guid jobId)
        {
            return await _context.EmailJobs
                .Include(j => j.Recipients)
                .Include(j => j.Creator)
                .FirstOrDefaultAsync(j => j.Id == jobId);
        }

        public async Task<List<EmailJob>> GetJobsAsync(Guid? userId = null, string? status = null)
        {
            var query = _context.EmailJobs.Include(j => j.Creator).AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(j => j.CreatedBy == userId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(j => j.Status == status);
            }

            return await query
                .OrderByDescending(j => j.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<bool> CancelJobAsync(Guid jobId)
        {
            var job = await _context.EmailJobs.FindAsync(jobId);
            if (job == null) return false;

            if (job.Status == EmailJobStatus.Completed || job.Status == EmailJobStatus.Cancelled)
            {
                return false; // Can't cancel completed or already cancelled jobs
            }

            job.Status = EmailJobStatus.Cancelled;
            job.UpdatedAt = DateTime.UtcNow;
            job.CompletedAt = DateTime.UtcNow;

            // Cancel all pending recipients
            var pendingRecipients = await _context.EmailJobRecipients
                .Where(r => r.JobId == jobId && r.Status == EmailJobStatus.Pending)
                .ToListAsync();

            foreach (var recipient in pendingRecipients)
            {
                recipient.Status = EmailJobStatus.Cancelled;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Cancelled email job {jobId}");
            return true;
        }

        public async Task<bool> PauseJobAsync(Guid jobId)
        {
            var job = await _context.EmailJobs.FindAsync(jobId);
            if (job == null) return false;

            if (job.Status != EmailJobStatus.Processing && job.Status != EmailJobStatus.Pending)
            {
                return false; // Can only pause pending or processing jobs
            }

            job.Status = EmailJobStatus.Paused;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Paused email job {jobId}");
            return true;
        }

        public async Task<bool> ResumeJobAsync(Guid jobId)
        {
            var job = await _context.EmailJobs.FindAsync(jobId);
            if (job == null) return false;

            if (job.Status != EmailJobStatus.Paused)
            {
                return false; // Can only resume paused jobs
            }

            job.Status = EmailJobStatus.Pending;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Resumed email job {jobId}");
            return true;
        }

        public async Task<EmailJobStats> GetJobStatsAsync(Guid? userId = null)
        {
            var query = _context.EmailJobs.AsQueryable();
            
            if (userId.HasValue)
            {
                query = query.Where(j => j.CreatedBy == userId.Value);
            }

            var stats = new EmailJobStats
            {
                TotalJobs = await query.CountAsync(),
                PendingJobs = await query.CountAsync(j => j.Status == EmailJobStatus.Pending),
                ProcessingJobs = await query.CountAsync(j => j.Status == EmailJobStatus.Processing),
                CompletedJobs = await query.CountAsync(j => j.Status == EmailJobStatus.Completed),
                FailedJobs = await query.CountAsync(j => j.Status == EmailJobStatus.Failed),
                TotalEmailsSent = await query.SumAsync(j => j.SuccessCount),
                TotalEmailsFailed = await query.SumAsync(j => j.FailedCount),
                RemainingQuota = await GetRemainingQuotaAsync()
            };

            return stats;
        }

        public async Task<int> GetRemainingQuotaAsync()
        {
            var today = DateTime.UtcNow.Date;
            var quota = await _context.EmailQuotas.FirstOrDefaultAsync(q => q.Date == today);
            
            if (quota == null)
            {
                // Create quota for today
                quota = new EmailQuota
                {
                    Date = today,
                    EmailsSent = 0,
                    QuotaLimit = DAILY_QUOTA
                };
                _context.EmailQuotas.Add(quota);
                await _context.SaveChangesAsync();
            }

            return Math.Max(0, quota.QuotaLimit - quota.EmailsSent);
        }

        public async Task<bool> CanSendEmailsAsync(int count)
        {
            var remaining = await GetRemainingQuotaAsync();
            return remaining >= count;
        }

        public async Task IncrementQuotaUsageAsync(int count)
        {
            var today = DateTime.UtcNow.Date;
            var quota = await _context.EmailQuotas.FirstOrDefaultAsync(q => q.Date == today);
            
            if (quota == null)
            {
                quota = new EmailQuota
                {
                    Date = today,
                    EmailsSent = count,
                    QuotaLimit = DAILY_QUOTA
                };
                _context.EmailQuotas.Add(quota);
            }
            else
            {
                quota.EmailsSent += count;
                quota.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }

    public class EmailJobStats
    {
        public int TotalJobs { get; set; }
        public int PendingJobs { get; set; }
        public int ProcessingJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
        public int TotalEmailsSent { get; set; }
        public int TotalEmailsFailed { get; set; }
        public int RemainingQuota { get; set; }
    }
}