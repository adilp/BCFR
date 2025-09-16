using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MemberOrgApi.Services;
using MemberOrgApi.Models;
using MemberOrgApi.DTOs;

namespace MemberOrgApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class EmailJobController : ControllerBase
    {
        private readonly IEmailJobService _jobService;
        private readonly ILogger<EmailJobController> _logger;

        public EmailJobController(IEmailJobService jobService, ILogger<EmailJobController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<ActionResult<EmailJobResponse>> CreateJob([FromBody] CreateEmailJobRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                // Validate request
                if (request.Recipients == null || !request.Recipients.Any())
                {
                    return BadRequest(new { message = "At least one recipient is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    return BadRequest(new { message = "Subject is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Body))
                {
                    return BadRequest(new { message = "Body is required" });
                }

                // Check quota
                var remainingQuota = await _jobService.GetRemainingQuotaAsync();
                if (remainingQuota < request.Recipients.Count)
                {
                    return BadRequest(new 
                    { 
                        message = $"Insufficient email quota. You can send {remainingQuota} more emails today. This job requires {request.Recipients.Count} emails.",
                        remainingQuota,
                        required = request.Recipients.Count
                    });
                }

                // Create the job
                var job = await _jobService.CreateJobAsync(
                    userId,
                    request.Subject,
                    request.Body,
                    request.IsHtml,
                    request.Recipients,
                    request.ScheduledFor
                );

                return Ok(new EmailJobResponse
                {
                    Id = job.Id,
                    Subject = job.Subject,
                    Status = job.Status,
                    TotalRecipients = job.TotalRecipients,
                    ProcessedCount = job.ProcessedCount,
                    SuccessCount = job.SuccessCount,
                    FailedCount = job.FailedCount,
                    ScheduledFor = job.ScheduledFor,
                    CreatedAt = job.CreatedAt,
                    Message = $"Email job created successfully. It will be processed shortly."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email job");
                return StatusCode(500, new { message = "Failed to create email job" });
            }
        }

        [HttpGet("{jobId}")]
        public async Task<ActionResult<EmailJobDetailResponse>> GetJob(Guid jobId)
        {
            var job = await _jobService.GetJobAsync(jobId);
            if (job == null)
            {
                return NotFound();
            }

            return Ok(new EmailJobDetailResponse
            {
                Id = job.Id,
                Subject = job.Subject,
                Body = job.Body,
                IsHtml = job.IsHtml,
                Status = job.Status,
                TotalRecipients = job.TotalRecipients,
                ProcessedCount = job.ProcessedCount,
                SuccessCount = job.SuccessCount,
                FailedCount = job.FailedCount,
                ScheduledFor = job.ScheduledFor,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                CreatedBy = job.Creator?.Email,
                Recipients = job.Recipients?.Select(r => new EmailRecipientStatus
                {
                    Email = r.Email,
                    Status = r.Status,
                    ProcessedAt = r.ProcessedAt,
                    ErrorMessage = r.ErrorMessage
                }).ToList()
            });
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<EmailJobResponse>>> GetJobs([FromQuery] string? status = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = null;
            
            // If not admin, only show their own jobs
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !string.IsNullOrEmpty(userIdClaim))
            {
                userId = Guid.Parse(userIdClaim);
            }

            var jobs = await _jobService.GetJobsAsync(userId, status);
            
            return Ok(jobs.Select(job => new EmailJobResponse
            {
                Id = job.Id,
                Subject = job.Subject,
                Status = job.Status,
                TotalRecipients = job.TotalRecipients,
                ProcessedCount = job.ProcessedCount,
                SuccessCount = job.SuccessCount,
                FailedCount = job.FailedCount,
                ScheduledFor = job.ScheduledFor,
                CreatedAt = job.CreatedAt,
                CreatedBy = job.Creator?.Email
            }).ToList());
        }

        [HttpPost("{jobId}/cancel")]
        public async Task<ActionResult> CancelJob(Guid jobId)
        {
            var success = await _jobService.CancelJobAsync(jobId);
            if (!success)
            {
                return BadRequest(new { message = "Cannot cancel this job. It may already be completed or cancelled." });
            }

            return Ok(new { message = "Job cancelled successfully" });
        }

        [HttpPost("{jobId}/pause")]
        public async Task<ActionResult> PauseJob(Guid jobId)
        {
            var success = await _jobService.PauseJobAsync(jobId);
            if (!success)
            {
                return BadRequest(new { message = "Cannot pause this job" });
            }

            return Ok(new { message = "Job paused successfully" });
        }

        [HttpPost("{jobId}/resume")]
        public async Task<ActionResult> ResumeJob(Guid jobId)
        {
            var success = await _jobService.ResumeJobAsync(jobId);
            if (!success)
            {
                return BadRequest(new { message = "Cannot resume this job" });
            }

            return Ok(new { message = "Job resumed successfully" });
        }

        [HttpGet("stats")]
        public async Task<ActionResult<EmailJobStats>> GetStats()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = null;
            
            // If not admin, only show their own stats
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !string.IsNullOrEmpty(userIdClaim))
            {
                userId = Guid.Parse(userIdClaim);
            }

            var stats = await _jobService.GetJobStatsAsync(userId);
            return Ok(stats);
        }

        [HttpGet("quota")]
        public async Task<ActionResult<QuotaResponse>> GetQuota()
        {
            var remaining = await _jobService.GetRemainingQuotaAsync();
            return Ok(new QuotaResponse
            {
                RemainingToday = remaining,
                DailyLimit = 100,
                UsedToday = 100 - remaining
            });
        }
    }

    // DTOs
    public class CreateEmailJobRequest
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; } = true;
        public List<string> Recipients { get; set; }
        public DateTime? ScheduledFor { get; set; }
    }

    public class EmailJobResponse
    {
        public Guid Id { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public int TotalRecipients { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? Message { get; set; }
    }

    public class EmailJobDetailResponse : EmailJobResponse
    {
        public string Body { get; set; }
        public bool IsHtml { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public List<EmailRecipientStatus> Recipients { get; set; }
    }

    public class EmailRecipientStatus
    {
        public string Email { get; set; }
        public string Status { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class QuotaResponse
    {
        public int RemainingToday { get; set; }
        public int DailyLimit { get; set; }
        public int UsedToday { get; set; }
    }
}