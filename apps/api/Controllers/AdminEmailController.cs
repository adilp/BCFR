using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MemberOrgApi.Services;
using MemberOrgApi.Models;

namespace MemberOrgApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminEmailController> _logger;

        public AdminEmailController(IEmailService emailService, ILogger<AdminEmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public class SendEmailRequest
        {
            public List<string> ToEmails { get; set; } = new List<string>();
            public List<string>? CcEmails { get; set; }
            public List<string>? BccEmails { get; set; }
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
            public bool IsHtml { get; set; } = true;
        }

        public class SendEmailResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int RecipientCount { get; set; }
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

                if (request.CcEmails != null)
                {
                    foreach (var email in request.CcEmails)
                    {
                        if (!IsValidEmail(email))
                        {
                            invalidEmails.Add(email);
                        }
                    }
                }

                if (request.BccEmails != null)
                {
                    foreach (var email in request.BccEmails)
                    {
                        if (!IsValidEmail(email))
                        {
                            invalidEmails.Add(email);
                        }
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
                    request.CcEmails,
                    request.BccEmails,
                    request.Subject,
                    request.Body,
                    request.IsHtml
                );

                if (result)
                {
                    var totalRecipients = request.ToEmails.Count + 
                                        (request.CcEmails?.Count ?? 0) + 
                                        (request.BccEmails?.Count ?? 0);
                    
                    _logger.LogInformation($"Admin sent email to {totalRecipients} recipients with subject: {request.Subject}");
                    
                    return Ok(new SendEmailResponse
                    {
                        Success = true,
                        Message = $"Email successfully sent to {totalRecipients} recipient(s)",
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