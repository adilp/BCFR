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

                // Build branded HTML body; if plain text provided, convert to HTML and wrap
                var htmlBody = BuildBrandedEmailHtml(request.Subject, request.Body, request.IsHtml);

                var campaignId = await _emailQueueService.QueueCampaignAsync(
                    campaignName: $"One-off Broadcast - {request.Subject} - {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
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

        private string BuildBrandedEmailHtml(string subject, string content, bool isHtml)
        {
            var bodyHtml = isHtml
                ? content
                : System.Net.WebUtility.HtmlEncode(content).Replace("\n", "<br/>");

            return $@"<!DOCTYPE html>
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>{System.Net.WebUtility.HtmlEncode(subject)} - BCFR</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif; line-height: 1.6; color: #212529; margin: 0; padding: 0; background-color: #fdf8f1; }}
    .wrapper {{ background-color: #fdf8f1; padding: 40px 20px; }}
    .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.07); }}
    .header {{ background: #6B3AA0; color: #fff; padding: 20px; }}
    .header h1 {{ margin: 0; font-size: 20px; }}
    .content {{ padding: 24px; }}
    .content h2 {{ margin-top: 0; font-size: 18px; color: #212529; }}
    .footer {{ background: #F5F2ED; color: #6b7280; padding: 16px; text-align: center; font-size: 12px; }}
    a.button {{ background:#4263EB; color:#fff; padding:12px 18px; border-radius:8px; text-decoration:none; display:inline-block; }}
  </style>
  <!-- Branding: BCFR purple header, light background, consistent typography -->
  </head>
  <body>
    <div class='wrapper'>
      <div class='container'>
        <div class='header'>
          <h1>Birmingham Committee on Foreign Relations</h1>
        </div>
        <div class='content'>
          {(string.IsNullOrWhiteSpace(subject) ? "" : $"<h2>{System.Net.WebUtility.HtmlEncode(subject)}</h2>")}
          {bodyHtml}
        </div>
        <div class='footer'>
          <p><strong>Birmingham Committee on Foreign Relations</strong></p>
          <p style='margin:4px 0'>Birmingham, AL</p>
          <p style='margin:8px 0 0 0;font-size:11px;color:#9CA3AF'>To unsubscribe, reply to this email with &quot;Unsubscribe&quot; in the subject line.</p>
        </div>
      </div>
    </div>
  </body>
</html>";
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
