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

namespace MemberOrgApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IEmailQueueService _emailQueueService;
        private readonly ILogger<AdminEmailController> _logger;

        public AdminEmailController(IEmailService emailService, IEmailQueueService emailQueueService, ILogger<AdminEmailController> logger)
        {
            _emailService = emailService;
            _emailQueueService = emailQueueService;
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

                // Build recipients list (dedupe by normalized email)
                var recipients = request.ToEmails
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .GroupBy(e => e.ToLowerInvariant())
                    .Select(g => new EmailRecipient { Email = g.First(), Name = null })
                    .ToList();

                // Ensure HTML body; if plain text provided, wrap it
                var htmlBody = request.IsHtml
                    ? request.Body
                    : $"<pre style=\"font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', monospace; white-space: pre-wrap;\">{System.Net.WebUtility.HtmlEncode(request.Body)}</pre>";

                var campaignId = await _emailQueueService.QueueCampaignAsync(
                    campaignName: $"One-off Broadcast - {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
                    campaignType: "Broadcast",
                    recipients: recipients,
                    subject: request.Subject,
                    htmlBody: htmlBody
                );

                var totalRecipients = recipients.Count;
                _logger.LogInformation("Queued broadcast campaign {CampaignId} to {Count} recipients", campaignId, totalRecipients);

                return Ok(new SendEmailResponse
                {
                    Success = true,
                    Message = $"Email queued to {totalRecipients} recipient(s) (Campaign {campaignId})",
                    RecipientCount = totalRecipients
                });
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
