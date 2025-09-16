using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MemberOrgApi.Services;
using MemberOrgApi.Models;
using MemberOrgApi.DTOs;
using System.Text;
using System.Text.Json;
using System.Security.Claims;

namespace MemberOrgApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IEmailJobService _jobService;
        private readonly ILogger<AdminEmailController> _logger;

        public AdminEmailController(IEmailService emailService, IEmailJobService jobService, ILogger<AdminEmailController> logger)
        {
            _emailService = emailService;
            _jobService = jobService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<SendEmailResponse>> SendEmail([FromBody] SendEmailRequest request)
        {
            try
            {
                if (request.ToEmails == null || !request.ToEmails.Any())
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = "At least one recipient email is required" 
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = "Email subject is required" 
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Body))
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = "Email body is required" 
                    });
                }

                var invalidEmails = new List<string>();
                
                foreach (var email in request.ToEmails)
                {
                    if (!IsValidEmail(email))
                    {
                        invalidEmails.Add(email);
                    }
                }

                if (invalidEmails.Any())
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = $"Invalid email addresses: {string.Join(", ", invalidEmails)}" 
                    });
                }

                var result = await _emailService.SendBroadcastEmailAsync(
                    request.ToEmails,
                    request.Subject,
                    request.Body,
                    request.IsHtml
                );

                if (result)
                {
                    var totalRecipients = request.ToEmails.Count;
                    
                    _logger.LogInformation($"Admin sent individual emails to {totalRecipients} recipients with subject: {request.Subject}");
                    
                    return Ok(new SendEmailResponse
                    {
                        Success = true,
                        Message = $"Email successfully sent individually to {totalRecipients} recipient(s)",
                        RecipientCount = totalRecipients
                    });
                }
                else
                {
                    return StatusCode(500, new SendEmailResponse
                    {
                        Success = false,
                        Message = "Failed to send email. Please try again later."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending broadcast email");
                return StatusCode(500, new SendEmailResponse
                {
                    Success = false,
                    Message = "An error occurred while sending the email"
                });
            }
        }

        [HttpPost("send-with-progress")]
        public async Task SendEmailWithProgress([FromBody] SendEmailRequest request)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("X-Accel-Buffering", "no");

            var totalRecipients = request.ToEmails.Count;
            
            // Send initial event with total count
            await WriteSSE($"{{\"total\": {totalRecipients}, \"sent\": 0}}");

            var progress = 0;
            var result = await _emailService.SendBroadcastEmailWithProgressAsync(
                request.ToEmails,
                request.Subject,
                request.Body,
                request.IsHtml,
                (sentCount) =>
                {
                    progress = sentCount;
                    // Send progress update
                    _ = WriteSSE($"{{\"total\": {totalRecipients}, \"sent\": {sentCount}}}");
                }
            );

            // Send final result
            if (result)
            {
                await WriteSSE($"{{\"total\": {totalRecipients}, \"sent\": {totalRecipients}, \"complete\": true, \"success\": true}}");
            }
            else
            {
                await WriteSSE($"{{\"total\": {totalRecipients}, \"sent\": {progress}, \"complete\": true, \"success\": false}}");
            }
        }

        private async Task WriteSSE(string data)
        {
            var message = $"data: {data}\n\n";
            var bytes = Encoding.UTF8.GetBytes(message);
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
            await Response.Body.FlushAsync();
        }

        [HttpPost("queue")]
        public async Task<ActionResult<SendEmailResponse>> QueueEmail([FromBody] SendEmailRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                if (request.ToEmails == null || !request.ToEmails.Any())
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = "At least one recipient email is required" 
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = "Email subject is required" 
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Body))
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = "Email body is required" 
                    });
                }

                // Check quota
                var remainingQuota = await _jobService.GetRemainingQuotaAsync();
                if (remainingQuota < request.ToEmails.Count)
                {
                    return BadRequest(new SendEmailResponse 
                    { 
                        Success = false, 
                        Message = $"Insufficient email quota. You can send {remainingQuota} more emails today. This job requires {request.ToEmails.Count} emails."
                    });
                }

                // Create the job
                var job = await _jobService.CreateJobAsync(
                    userId,
                    request.Subject,
                    request.Body,
                    request.IsHtml,
                    request.ToEmails,
                    null // Not scheduled
                );

                return Ok(new SendEmailResponse
                {
                    Success = true,
                    Message = $"Email job created successfully with {request.ToEmails.Count} recipients. Job ID: {job.Id}",
                    RecipientCount = request.ToEmails.Count,
                    JobId = job.Id.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing email job");
                return StatusCode(500, new SendEmailResponse
                {
                    Success = false,
                    Message = "An error occurred while queuing the email"
                });
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

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
    }
}