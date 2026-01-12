using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Security.Cryptography;

namespace MemberOrgApi.Services
{
    // Simple wrapper to convert IOptions to IOptionsSnapshot
    public class OptionsSnapshotWrapper<T> : IOptionsSnapshot<T> where T : class
    {
        private readonly IOptions<T> _options;
        
        public OptionsSnapshotWrapper(IOptions<T> options)
        {
            _options = options;
        }
        
        public T Value => _options.Value;
        public T Get(string? name) => _options.Value;
    }

    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            
            var apiKey = _configuration["Resend:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Resend API key is not configured");
            }

            var optionsWrapper = Options.Create(new ResendClientOptions
            {
                ApiToken = apiKey
            });
            var optionsSnapshot = new OptionsSnapshotWrapper<ResendClientOptions>(optionsWrapper);
            _resend = new ResendClient(optionsSnapshot, _httpClient);
            _fromEmail = _configuration["Resend:FromEmail"] ?? "no-reply@yourdomain.com";
            _fromName = _configuration["Resend:FromName"] ?? "Member Organization";
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName, string lastName)
        {
            try
            {
                var htmlBody = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body { 
                                font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
                                line-height: 1.6;
                                color: #212529;
                                margin: 0;
                                padding: 0;
                                background-color: #fdf8f1;
                            }
                            .wrapper {
                                background-color: #fdf8f1;
                                padding: 40px 20px;
                            }
                            .container { 
                                max-width: 600px;
                                margin: 0 auto;
                                background-color: #ffffff;
                                border-radius: 12px;
                                overflow: hidden;
                                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.07);
                            }
                            .header { 
                                background: #6B3AA0;
                                color: white;
                                padding: 48px 32px;
                                text-align: center;
                            }
                            .header h1 {
                                margin: 0;
                                font-size: 32px;
                                font-weight: 700;
                                letter-spacing: -0.02em;
                            }
                            .content { 
                                padding: 40px 32px;
                                background-color: #ffffff;
                            }
                            .content h2 {
                                color: #212529;
                                font-size: 24px;
                                font-weight: 600;
                                margin: 0 0 16px 0;
                                letter-spacing: -0.01em;
                            }
                            .content p {
                                color: #495057;
                                font-size: 16px;
                                line-height: 1.6;
                                margin: 0 0 16px 0;
                            }
                            .content ul {
                                color: #495057;
                                font-size: 16px;
                                line-height: 1.8;
                                margin: 24px 0;
                                padding-left: 24px;
                            }
                            .content li {
                                margin-bottom: 8px;
                            }
                            .button { 
                                display: inline-block;
                                padding: 14px 32px;
                                background: #FFC833;
                                color: #212529;
                                text-decoration: none;
                                border-radius: 8px;
                                font-weight: 600;
                                font-size: 16px;
                                margin: 32px 0;
                                transition: all 200ms ease;
                            }
                            .button:hover {
                                background: #FFD45C;
                            }
                            .footer { 
                                background-color: #F5F2ED;
                                padding: 32px;
                                text-align: center;
                            }
                            .footer p {
                                color: #6C757D;
                                font-size: 14px;
                                margin: 0 0 8px 0;
                            }
                            .footer a {
                                color: #4263EB;
                                text-decoration: none;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='wrapper'>
                            <div class='container'>
                                <div class='header'>
                                    <h1>Welcome to BCFR!</h1>
                                </div>
                                <div class='content'>
                                    <h2>Hello " + firstName + " " + lastName + @",</h2>
                                    <p>Thank you for joining the Birmingham Committee on Foreign Relations! We're thrilled to have you as part of our distinguished community of global affairs enthusiasts.</p>
                                    <p>As a BCFR member, you now have access to:</p>
                                    <ul>
                                        <li>Exclusive speaker events with world-renowned experts</li>
                                        <li>Thought-provoking discussions on international affairs</li>
                                        <li>Networking opportunities with Birmingham's thought leaders</li>
                                        <li>Members-only resources and educational content</li>
                                    </ul>
                                    <p>Ready to explore? Log in to your account to view upcoming events, access member resources, and connect with fellow members.</p>
                                    <center>
                                        <a href='" + _configuration["App:BaseUrl"] + @"/login' class='button'>Access Your Member Portal</a>
                                    </center>
                                    <p>If you have any questions, our team is here to help at <a href='mailto:admin@birminghamforeignrelations.org'>admin@birminghamforeignrelations.org</a></p>
                                    <p>Best regards,<br/><strong>The BCFR Team</strong></p>
                                </div>
                                <div class='footer'>
                                    <p><strong>Birmingham Committee on Foreign Relations</strong></p>
                                    <p>© 2025 BCFR. All rights reserved.</p>
                                    <p>You received this email because you registered at <a href='https://birminghamforeignrelations.org'>birminghamforeignrelations.org</a></p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                var textBody = "Welcome to BCFR!\n\n" +
                    "Hello " + firstName + " " + lastName + ",\n\n" +
                    "Thank you for joining the Birmingham Committee on Foreign Relations! We're thrilled to have you as part of our distinguished community.\n\n" +
                    "As a BCFR member, you now have access to:\n" +
                    "- Exclusive speaker events with world-renowned experts\n" +
                    "- Thought-provoking discussions on international affairs\n" +
                    "- Networking opportunities with Birmingham's thought leaders\n" +
                    "- Members-only resources and educational content\n\n" +
                    "To get started, log in to your account at " + _configuration["App:BaseUrl"] + "/login\n\n" +
                    "If you have any questions, feel free to reach out to us at info@birminghamforeignrelations.org\n\n" +
                    "Best regards,\n" +
                    "The BCFR Team";

                var message = new EmailMessage
                {
                    From = _fromName + " <" + _fromEmail + ">",
                    To = toEmail,
                    Subject = "Welcome to Birmingham Council on Foreign Relations",
                    HtmlBody = htmlBody,
                    TextBody = textBody
                };

                _logger.LogInformation("Sending welcome email to {Email}, Name: {FirstName} {LastName}",
                    toEmail, firstName, lastName);

                var response = await _resend.EmailSendAsync(message);

                _logger.LogInformation("Welcome email sent successfully to {Email}",
                    toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}, Name: {FirstName} {LastName}",
                    toEmail, firstName, lastName);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            try
            {
                _logger.LogInformation("Sending password reset email to {Email}", toEmail);

                var resetUrl = _configuration["App:BaseUrl"] + "/reset-password?token=" + resetToken;
                
                var htmlBody = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body { 
                                font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
                                line-height: 1.6;
                                color: #212529;
                                margin: 0;
                                padding: 0;
                                background-color: #fdf8f1;
                            }
                            .wrapper {
                                background-color: #fdf8f1;
                                padding: 40px 20px;
                            }
                            .container { 
                                max-width: 600px;
                                margin: 0 auto;
                                background-color: #ffffff;
                                border-radius: 12px;
                                overflow: hidden;
                                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.07);
                            }
                            .header { 
                                background: #1E0E31;
                                color: white;
                                padding: 48px 32px;
                                text-align: center;
                            }
                            .header h1 {
                                margin: 0;
                                font-size: 32px;
                                font-weight: 700;
                                letter-spacing: -0.02em;
                            }
                            .content { 
                                padding: 40px 32px;
                                background-color: #ffffff;
                            }
                            .content h2 {
                                color: #212529;
                                font-size: 24px;
                                font-weight: 600;
                                margin: 0 0 16px 0;
                            }
                            .content p {
                                color: #495057;
                                font-size: 16px;
                                line-height: 1.6;
                                margin: 0 0 16px 0;
                            }
                            .alert-box {
                                background-color: #FFF4E6;
                                border-left: 4px solid #FFC833;
                                padding: 16px;
                                margin: 24px 0;
                                border-radius: 4px;
                            }
                            .button { 
                                display: inline-block;
                                padding: 14px 32px;
                                background: #4263EB;
                                color: white;
                                text-decoration: none;
                                border-radius: 8px;
                                font-weight: 600;
                                font-size: 16px;
                                margin: 32px 0;
                            }
                            .footer { 
                                background-color: #F5F2ED;
                                padding: 32px;
                                text-align: center;
                            }
                            .footer p {
                                color: #6C757D;
                                font-size: 14px;
                                margin: 0 0 8px 0;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='wrapper'>
                            <div class='container'>
                                <div class='header'>
                                    <h1>Password Reset Request</h1>
                                </div>
                                <div class='content'>
                                    <h2>Reset Your BCFR Account Password</h2>
                                    <p>We received a request to reset the password for your Birmingham Council on Foreign Relations account.</p>
                                    <center>
                                        <a href='" + resetUrl + @"' class='button'>Reset My Password</a>
                                    </center>
                                    <div class='alert-box'>
                                        <p style='margin: 0;'><strong>Security Notice:</strong></p>
                                        <p style='margin: 8px 0 0 0;'>This link will expire in 1 hour for your security. If you didn't request this password reset, please ignore this email and your password will remain unchanged.</p>
                                    </div>
                                    <p>For additional assistance, contact us at <a href='mailto:info@birminghamforeignrelations.org'>info@birminghamforeignrelations.org</a></p>
                                </div>
                                <div class='footer'>
                                    <p><strong>Birmingham Council on Foreign Relations</strong></p>
                                    <p>© 2025 BCFR. All rights reserved.</p>
                                    <p>This is a security-related email for your BCFR account.</p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                var message = new EmailMessage
                {
                    From = _fromName + " <" + _fromEmail + ">",
                    To = toEmail,
                    Subject = "BCFR Password Reset Request",
                    HtmlBody = htmlBody
                };

                await _resend.EmailSendAsync(message);
                _logger.LogInformation("Password reset email sent to " + toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to " + toEmail);
                return false;
            }
        }

        public async Task<bool> SendMembershipConfirmationAsync(string toEmail, string membershipType, string transactionId)
        {
            try
            {
                var validUntil = DateTime.Now.AddYears(1).ToString("MMMM dd, yyyy");
                
                var htmlBody = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body { 
                                font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
                                line-height: 1.6;
                                color: #212529;
                                margin: 0;
                                padding: 0;
                                background-color: #fdf8f1;
                            }
                            .wrapper {
                                background-color: #fdf8f1;
                                padding: 40px 20px;
                            }
                            .container { 
                                max-width: 600px;
                                margin: 0 auto;
                                background-color: #ffffff;
                                border-radius: 12px;
                                overflow: hidden;
                                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.07);
                            }
                            .header { 
                                background: #FFC833;
                                color: #212529;
                                padding: 48px 32px;
                                text-align: center;
                            }
                            .header h1 {
                                margin: 0;
                                font-size: 32px;
                                font-weight: 700;
                                letter-spacing: -0.02em;
                            }
                            .content { 
                                padding: 40px 32px;
                            }
                            .content h2 {
                                color: #212529;
                                font-size: 24px;
                                font-weight: 600;
                                margin: 0 0 16px 0;
                            }
                            .content p {
                                color: #495057;
                                font-size: 16px;
                                line-height: 1.6;
                                margin: 0 0 16px 0;
                            }
                            .info-box { 
                                background: #F5F2ED;
                                padding: 24px;
                                border-radius: 8px;
                                margin: 32px 0;
                                border: 1px solid #E8E0D6;
                            }
                            .info-box p {
                                margin: 0 0 12px 0;
                                font-size: 15px;
                            }
                            .info-box p:last-child {
                                margin: 0;
                            }
                            .label {
                                color: #6C757D;
                                font-weight: 600;
                                display: inline-block;
                                min-width: 140px;
                            }
                            .value {
                                color: #212529;
                                font-weight: 400;
                            }
                            .footer { 
                                background-color: #F5F2ED;
                                padding: 32px;
                                text-align: center;
                            }
                            .footer p {
                                color: #6C757D;
                                font-size: 14px;
                                margin: 0 0 8px 0;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='wrapper'>
                            <div class='container'>
                                <div class='header'>
                                    <h1>🎉 Membership Confirmed!</h1>
                                </div>
                                <div class='content'>
                                    <h2>Welcome to BCFR Membership</h2>
                                    <p>Congratulations! Your Birmingham Council on Foreign Relations membership has been successfully activated.</p>
                                    <div class='info-box'>
                                        <p><span class='label'>Membership Type:</span> <span class='value'>" + membershipType + @"</span></p>
                                        <p><span class='label'>Transaction ID:</span> <span class='value'>" + transactionId + @"</span></p>
                                        <p><span class='label'>Valid Until:</span> <span class='value'>" + validUntil + @"</span></p>
                                    </div>
                                    <p>Your membership benefits are now active! You can access exclusive events, member resources, and connect with our distinguished community of global affairs enthusiasts.</p>
                                    <p>Visit your member portal to explore upcoming events and exclusive content.</p>
                                    <p>Thank you for your commitment to fostering international understanding and dialogue.</p>
                                </div>
                                <div class='footer'>
                                    <p><strong>Birmingham Council on Foreign Relations</strong></p>
                                    <p>© 2025 BCFR. All rights reserved.</p>
                                    <p>This email serves as your membership confirmation receipt.</p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                var message = new EmailMessage
                {
                    From = _fromName + " <" + _fromEmail + ">",
                    To = toEmail,
                    Subject = "BCFR Membership Confirmation - Welcome!",
                    HtmlBody = htmlBody
                };

                await _resend.EmailSendAsync(message);
                _logger.LogInformation("Membership confirmation email sent to " + toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send membership confirmation email to " + toEmail);
                return false;
            }
        }

        public async Task<bool> SendEventRegistrationConfirmationAsync(string toEmail, string eventName, DateTime eventDate)
        {
            try
            {
                var eventDateFormatted = eventDate.ToString("MMMM dd, yyyy");
                var eventTimeFormatted = eventDate.ToString("hh:mm tt");
                
                var htmlBody = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body { 
                                font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
                                line-height: 1.6;
                                color: #212529;
                                margin: 0;
                                padding: 0;
                                background-color: #fdf8f1;
                            }
                            .wrapper {
                                background-color: #fdf8f1;
                                padding: 40px 20px;
                            }
                            .container { 
                                max-width: 600px;
                                margin: 0 auto;
                                background-color: #ffffff;
                                border-radius: 12px;
                                overflow: hidden;
                                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.07);
                            }
                            .header { 
                                background: #6B3AA0;
                                color: white;
                                padding: 48px 32px;
                                text-align: center;
                            }
                            .header h1 {
                                margin: 0;
                                font-size: 32px;
                                font-weight: 700;
                                letter-spacing: -0.02em;
                            }
                            .content { 
                                padding: 40px 32px;
                            }
                            .content h2 {
                                color: #212529;
                                font-size: 24px;
                                font-weight: 600;
                                margin: 0 0 16px 0;
                            }
                            .content p {
                                color: #495057;
                                font-size: 16px;
                                line-height: 1.6;
                                margin: 0 0 16px 0;
                            }
                            .event-card { 
                                background: linear-gradient(135deg, #F5F2ED 0%, #FFFFFF 100%);
                                padding: 32px;
                                border-radius: 12px;
                                margin: 32px 0;
                                border: 2px solid #4263EB;
                                text-align: center;
                            }
                            .event-card h3 {
                                color: #212529;
                                font-size: 22px;
                                font-weight: 700;
                                margin: 0 0 20px 0;
                            }
                            .event-detail {
                                display: flex;
                                justify-content: center;
                                align-items: center;
                                gap: 8px;
                                margin: 12px 0;
                                font-size: 16px;
                            }
                            .event-icon {
                                font-size: 20px;
                            }
                            .event-label {
                                color: #6C757D;
                                font-weight: 600;
                            }
                            .event-value {
                                color: #212529;
                            }
                            .button { 
                                display: inline-block;
                                padding: 14px 32px;
                                background: #4263EB;
                                color: white;
                                text-decoration: none;
                                border-radius: 8px;
                                font-weight: 600;
                                font-size: 16px;
                                margin: 24px 0;
                            }
                            .footer { 
                                background-color: #F5F2ED;
                                padding: 32px;
                                text-align: center;
                            }
                            .footer p {
                                color: #6C757D;
                                font-size: 14px;
                                margin: 0 0 8px 0;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='wrapper'>
                            <div class='container'>
                                <div class='header'>
                                    <h1>Registration Confirmed!</h1>
                                </div>
                                <div class='content'>
                                    <h2>You're All Set!</h2>
                                    <p>Thank you for registering! Your spot has been reserved for this exciting BCFR event.</p>
                                    <div class='event-card'>
                                        <h3>" + eventName + @"</h3>
                                        <div class='event-detail'>
                                            <span class='event-icon'>📅</span>
                                            <span class='event-label'>Date:</span>
                                            <span class='event-value'>" + eventDateFormatted + @"</span>
                                        </div>
                                        <div class='event-detail'>
                                            <span class='event-icon'>🕐</span>
                                            <span class='event-label'>Time:</span>
                                            <span class='event-value'>" + eventTimeFormatted + @"</span>
                                        </div>
                                    </div>
                                    <p>We're looking forward to seeing you there! A reminder will be sent 24 hours before the event.</p>
                                    <p><strong>What to expect:</strong> Engaging discussions, expert insights, and valuable networking opportunities with Birmingham's international affairs community.</p>
                                    <center>
                                        <a href='" + _configuration["App:BaseUrl"] + @"/events' class='button'>View All Events</a>
                                    </center>
                                </div>
                                <div class='footer'>
                                    <p><strong>Birmingham Council on Foreign Relations</strong></p>
                                    <p>© 2025 BCFR. All rights reserved.</p>
                                    <p>You're receiving this because you registered for a BCFR event.</p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                var message = new EmailMessage
                {
                    From = _fromName + " <" + _fromEmail + ">",
                    To = toEmail,
                    Subject = "BCFR Event Registration Confirmed: " + eventName,
                    HtmlBody = htmlBody
                };

                await _resend.EmailSendAsync(message);
                _logger.LogInformation("Event registration email sent to " + toEmail + " for event " + eventName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event registration email to " + toEmail);
                return false;
            }
        }

        public async Task<bool> SendCustomEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null, List<EmailAttachment>? attachments = null)
        {
            try
            {
                // Unsubscribe email - users can reply to unsubscribe
                var unsubscribeEmail = _configuration["Resend:UnsubscribeEmail"] ?? "admin@birminghamforeignrelations.org";

                var message = new EmailMessage
                {
                    From = _fromName + " <" + _fromEmail + ">",
                    To = toEmail,
                    Subject = subject,
                    HtmlBody = htmlBody,
                    TextBody = textBody,
                    Headers = new Dictionary<string, string>
                    {
                        { "List-Unsubscribe", $"<mailto:{unsubscribeEmail}?subject=Unsubscribe>" },
                        { "List-Unsubscribe-Post", "List-Unsubscribe=One-Click" }
                    }
                };

                // Map optional attachments if provided
                if (attachments != null && attachments.Count > 0)
                {
                    var resendAttachments = new List<Resend.EmailAttachment>();
                    foreach (var a in attachments)
                    {
                        resendAttachments.Add(new Resend.EmailAttachment
                        {
                            Filename = a.FileName,
                            Content = Convert.FromBase64String(a.Base64Content)
                        });
                    }
                    message.Attachments = resendAttachments;
                }

                _logger.LogInformation("Sending custom email to {Email}, Subject: {Subject}, HasAttachments: {HasAttachments}",
                    toEmail, subject, attachments?.Count > 0);

                var response = await _resend.EmailSendAsync(message);

                _logger.LogInformation("Custom email sent successfully to {Email}, Subject: {Subject}",
                    toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send custom email. To: {Email}, Subject: {Subject}",
                    toEmail, subject);
                return false;
            }
        }

        public async Task<bool> SendBroadcastEmailAsync(List<string> toEmails, string subject, string bodyContent, bool isHtml = true)
        {
            try
            {
                var wrappedHtmlBody = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body { 
                                font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
                                line-height: 1.6;
                                color: #212529;
                                margin: 0;
                                padding: 0;
                                background-color: #fdf8f1;
                            }
                            .wrapper {
                                background-color: #fdf8f1;
                                padding: 40px 20px;
                            }
                            .container { 
                                max-width: 600px;
                                margin: 0 auto;
                                background-color: #ffffff;
                                border-radius: 12px;
                                overflow: hidden;
                                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.07);
                            }
                            .header { 
                                background: #6B3AA0;
                                color: white;
                                padding: 48px 32px;
                                text-align: center;
                            }
                            .header h1 {
                                margin: 0;
                                font-size: 32px;
                                font-weight: 700;
                                letter-spacing: -0.02em;
                            }
                            .content { 
                                padding: 40px 32px;
                                background-color: #ffffff;
                            }
                            .content h1 {
                                color: #212529;
                                font-size: 28px;
                                font-weight: 700;
                                margin: 0 0 16px 0;
                                letter-spacing: -0.01em;
                            }
                            .content h2 {
                                color: #212529;
                                font-size: 24px;
                                font-weight: 600;
                                margin: 0 0 16px 0;
                                letter-spacing: -0.01em;
                            }
                            .content h3 {
                                color: #212529;
                                font-size: 20px;
                                font-weight: 600;
                                margin: 0 0 12px 0;
                            }
                            .content p {
                                color: #495057;
                                font-size: 16px;
                                line-height: 1.6;
                                margin: 0 0 16px 0;
                            }
                            .content ul, .content ol {
                                color: #495057;
                                font-size: 16px;
                                line-height: 1.8;
                                margin: 16px 0;
                                padding-left: 24px;
                            }
                            .content li {
                                margin-bottom: 8px;
                            }
                            .content a {
                                color: #4263EB;
                                text-decoration: none;
                            }
                            .content a:hover {
                                text-decoration: underline;
                            }
                            .content strong {
                                font-weight: 600;
                                color: #212529;
                            }
                            .content em {
                                font-style: italic;
                            }
                            .content blockquote {
                                border-left: 4px solid #FFC833;
                                padding-left: 20px;
                                margin: 20px 0;
                                color: #6C757D;
                                font-style: italic;
                            }
                            .button { 
                                display: inline-block;
                                padding: 14px 32px;
                                background: #FFC833;
                                color: #212529;
                                text-decoration: none;
                                border-radius: 8px;
                                font-weight: 600;
                                font-size: 16px;
                                margin: 24px 0;
                                transition: all 200ms ease;
                            }
                            .button:hover {
                                background: #FFD45C;
                            }
                            .footer { 
                                background-color: #F5F2ED;
                                padding: 32px;
                                text-align: center;
                            }
                            .footer p {
                                color: #6C757D;
                                font-size: 14px;
                                margin: 0 0 8px 0;
                            }
                            .footer a {
                                color: #4263EB;
                                text-decoration: none;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='wrapper'>
                            <div class='container'>
                                <div class='header'>
                                    <h1>Birmingham Committee on Foreign Relations</h1>
                                </div>
                                <div class='content'>
                                    " + bodyContent + @"
                                </div>
                                <div class='footer'>
                                    <p><strong>Birmingham Committee on Foreign Relations</strong></p>
                                    <p style='margin:4px 0;'>Birmingham, AL</p>
                                    <p>© 2025 BCFR. All rights reserved.</p>
                                    <p>You received this email as a member of BCFR.</p>
                                    <p><a href='" + _configuration["App:BaseUrl"] + @"'>Visit Our Website</a></p>
                                    <p style='margin-top:12px;font-size:12px;color:#9CA3AF;'>To unsubscribe, reply to this email with &quot;Unsubscribe&quot; in the subject line.</p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                var textBody = System.Text.RegularExpressions.Regex.Replace(bodyContent, "<.*?>", string.Empty);
                textBody = "Birmingham Committee on Foreign Relations\n\n" + textBody + "\n\n© 2025 BCFR. All rights reserved.\n\nTo unsubscribe, reply to this email with \"Unsubscribe\" in the subject line.";

                // Send individual emails to each recipient to maintain privacy
                // Add delay to respect Resend's rate limit (2 requests per second)
                var unsubscribeEmail = _configuration["Resend:UnsubscribeEmail"] ?? "admin@birminghamforeignrelations.org";

                foreach (var toEmail in toEmails)
                {
                    var message = new EmailMessage
                    {
                        From = _fromName + " <" + _fromEmail + ">",
                        To = toEmail,
                        Subject = subject,
                        HtmlBody = isHtml ? wrappedHtmlBody : null,
                        TextBody = !isHtml ? bodyContent : textBody,
                        Headers = new Dictionary<string, string>
                        {
                            { "List-Unsubscribe", $"<mailto:{unsubscribeEmail}?subject=Unsubscribe>" }
                        }
                    };

                    await _resend.EmailSendAsync(message);

                    // Resend rate limit: 2 requests per second
                    // Using 1 second delay to be extra safe with rate limiting
                    await Task.Delay(1000);
                }
                
                _logger.LogInformation($"Broadcast email sent to {toEmails.Count} individual recipients with subject: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send broadcast email to {toEmails.Count} recipients");
                return false;
            }
        }

        public async Task<bool> SendEventAnnouncementEmailAsync(string toEmail, string firstName, string eventTitle,
            string eventDescription, DateTime eventDate, TimeSpan startTime, TimeSpan endTime,
            string location, string speaker, DateTime rsvpDeadline, bool allowPlusOne, string rsvpToken)
        {
            try
            {
                // Format dates and times for display
                var eventDateFormatted = eventDate.ToString("dddd, MMMM dd, yyyy");
                var startTimeFormatted = DateTime.Today.Add(startTime).ToString("h:mm tt");
                var endTimeFormatted = DateTime.Today.Add(endTime).ToString("h:mm tt");
                var rsvpDeadlineFormatted = rsvpDeadline.ToString("MMMM dd, yyyy");

                // Base URL for RSVP actions - need to use API URL, not frontend URL
                var apiBaseUrl = _configuration["App:ApiUrl"] ?? "http://localhost:5001/api";
                var frontendBaseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5173";

                // Generate RSVP URLs with the token - these go directly to the API
                var rsvpYesUrl = $"{apiBaseUrl}/email-rsvp/respond?token={rsvpToken}&response=yes";
                var rsvpNoUrl = $"{apiBaseUrl}/email-rsvp/respond?token={rsvpToken}&response=no";
                var rsvpYesPlusOneUrl = $"{apiBaseUrl}/email-rsvp/respond?token={rsvpToken}&response=yes&plusOne=true";

                var htmlBody = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {
                                font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
                                line-height: 1.6;
                                color: #212529;
                                margin: 0;
                                padding: 0;
                                background-color: #fdf8f1;
                            }
                            .wrapper {
                                background-color: #fdf8f1;
                                padding: 40px 20px;
                            }
                            .container {
                                max-width: 680px;
                                margin: 0 auto;
                                background-color: #ffffff;
                                border-radius: 16px;
                                overflow: hidden;
                                box-shadow: 0 6px 20px rgba(0, 0, 0, 0.08);
                            }
                            .header {
                                background: linear-gradient(135deg, #6B3AA0 0%, #4263EB 100%);
                                color: white;
                                padding: 60px 40px;
                                text-align: center;
                            }
                            .header h1 {
                                margin: 0 0 12px 0;
                                font-size: 36px;
                                font-weight: 700;
                                letter-spacing: -0.02em;
                            }
                            .header p {
                                margin: 0;
                                font-size: 18px;
                                opacity: 0.95;
                            }
                            .content {
                                padding: 48px 40px;
                                background-color: #ffffff;
                            }
                            .greeting {
                                font-size: 20px;
                                color: #212529;
                                font-weight: 600;
                                margin: 0 0 24px 0;
                            }
                            .event-card {
                                background: #FFFFFF;
                                border: 2px solid #E8E0D6;
                                border-radius: 12px;
                                padding: 32px;
                                margin: 32px 0;
                            }
                            .event-title {
                                color: #6B3AA0;
                                font-size: 28px;
                                font-weight: 700;
                                margin: 0 0 16px 0;
                                line-height: 1.3;
                            }
                            .event-description {
                                color: #495057;
                                font-size: 17px;
                                line-height: 1.6;
                                margin: 0 0 28px 0;
                            }
                            .event-details {
                                background: #F5F2ED;
                                border-radius: 8px;
                                padding: 24px;
                                margin: 24px 0;
                            }
                            .detail-row {
                                display: flex;
                                align-items: flex-start;
                                margin: 0 0 16px 0;
                                font-size: 16px;
                            }
                            .detail-row:last-child {
                                margin: 0;
                            }
                            .detail-icon {
                                font-size: 20px;
                                margin-right: 12px;
                                color: #6B3AA0;
                                min-width: 24px;
                            }
                            .detail-label {
                                color: #6C757D;
                                font-weight: 600;
                                min-width: 100px;
                                margin-right: 12px;
                            }
                            .detail-value {
                                color: #212529;
                                flex: 1;
                            }
                            .speaker-section {
                                background: linear-gradient(135deg, #FFF4E6 0%, #FFFFFF 100%);
                                border-left: 4px solid #FFC833;
                                padding: 20px 24px;
                                margin: 28px 0;
                                border-radius: 0 8px 8px 0;
                            }
                            .speaker-label {
                                color: #6C757D;
                                font-size: 14px;
                                text-transform: uppercase;
                                font-weight: 600;
                                letter-spacing: 0.5px;
                                margin: 0 0 8px 0;
                            }
                            .speaker-name {
                                color: #212529;
                                font-size: 18px;
                                font-weight: 600;
                                margin: 0;
                            }
                            .rsvp-section {
                                background: #F5F2ED;
                                border-radius: 12px;
                                padding: 32px;
                                margin: 32px 0;
                                text-align: center;
                            }
                            .rsvp-title {
                                color: #212529;
                                font-size: 22px;
                                font-weight: 600;
                                margin: 0 0 8px 0;
                            }
                            .rsvp-subtitle {
                                color: #6C757D;
                                font-size: 16px;
                                margin: 0 0 24px 0;
                            }
                            .rsvp-buttons {
                                display: block;
                                margin: 0 0 20px 0;
                            }
                            .btn-rsvp {
                                display: inline-block;
                                padding: 16px 40px;
                                margin: 0 8px 12px 8px;
                                text-decoration: none;
                                font-weight: 600;
                                font-size: 16px;
                                border-radius: 8px;
                                transition: all 200ms ease;
                                text-align: center;
                                min-width: 140px;
                            }
                            .btn-yes {
                                background: #22C55E;
                                color: white;
                            }
                            .btn-yes:hover {
                                background: #16A34A;
                                transform: translateY(-1px);
                                box-shadow: 0 4px 12px rgba(34, 197, 94, 0.3);
                            }
                            .btn-no {
                                background: #EF4444;
                                color: white;
                            }
                            .btn-no:hover {
                                background: #DC2626;
                                transform: translateY(-1px);
                                box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
                            }
                            .btn-plus-one {
                                background: #FFC833;
                                color: #212529;
                            }
                            .btn-plus-one:hover {
                                background: #FFD45C;
                                transform: translateY(-1px);
                                box-shadow: 0 4px 12px rgba(255, 200, 51, 0.3);
                            }
                            .plus-one-text {
                                color: #6C757D;
                                font-size: 14px;
                                margin: 20px 0 0 0;
                            }
                            .deadline-warning {
                                background: #FFF4E6;
                                border: 1px solid #FFD98D;
                                border-radius: 8px;
                                padding: 16px;
                                margin: 24px 0;
                                text-align: center;
                            }
                            .deadline-warning p {
                                margin: 0;
                                color: #B45309;
                                font-size: 15px;
                                font-weight: 500;
                            }
                            .footer {
                                background-color: #F5F2ED;
                                padding: 32px;
                                text-align: center;
                            }
                            .footer p {
                                color: #6C757D;
                                font-size: 14px;
                                margin: 0 0 8px 0;
                            }
                            .footer a {
                                color: #4263EB;
                                text-decoration: none;
                            }
                            @media only screen and (max-width: 600px) {
                                .container {
                                    border-radius: 0;
                                }
                                .header {
                                    padding: 40px 24px;
                                }
                                .header h1 {
                                    font-size: 28px;
                                }
                                .content {
                                    padding: 32px 24px;
                                }
                                .event-card {
                                    padding: 24px;
                                }
                                .btn-rsvp {
                                    display: block;
                                    width: 100%;
                                    margin: 0 0 12px 0;
                                }
                            }
                        </style>
                    </head>
                    <body>
                        <div class='wrapper'>
                            <div class='container'>
                                <div class='header'>
                                    <h1>📢 New Event Announcement</h1>
                                    <p>Birmingham Committee on Foreign Relations</p>
                                </div>
                                <div class='content'>
                                    <p class='greeting'>Dear " + firstName + @",</p>
                                    <p style='color: #495057; font-size: 17px; margin: 0 0 24px 0;'>We're excited to invite you to our upcoming event!</p>

                                    <div class='event-card'>
                                        <h2 class='event-title'>" + eventTitle + @"</h2>
                                        <p class='event-description'>" + eventDescription + @"</p>

                                        <div class='event-details'>
                                            <div class='detail-row'>
                                                <span class='detail-icon'>📅</span>
                                                <span class='detail-label'>Date:</span>
                                                <span class='detail-value'>" + eventDateFormatted + @"</span>
                                            </div>
                                            <div class='detail-row'>
                                                <span class='detail-icon'>⏰</span>
                                                <span class='detail-label'>Time:</span>
                                                <span class='detail-value'>" + startTimeFormatted + " - " + endTimeFormatted + @"</span>
                                            </div>
                                            <div class='detail-row'>
                                                <span class='detail-icon'>📍</span>
                                                <span class='detail-label'>Location:</span>
                                                <span class='detail-value'>" + location + @"</span>
                                            </div>
                                        </div>

                                        <div class='speaker-section'>
                                            <p class='speaker-label'>Featured Speaker</p>
                                            <p class='speaker-name'>" + speaker + @"</p>
                                        </div>
                                    </div>

                                    <div class='rsvp-section'>
                                        <h3 class='rsvp-title'>Will you be joining us?</h3>
                                        <p class='rsvp-subtitle'>Click below to RSVP directly from this email</p>

                                        <div class='rsvp-buttons'>
                                            <a href='" + rsvpYesUrl + @"' class='btn-rsvp btn-yes'>✓ Yes, I'll Attend</a>
                                            <a href='" + rsvpNoUrl + @"' class='btn-rsvp btn-no'>✗ No, Can't Make It</a>
                                        </div>
                                        " + (allowPlusOne ? @"
                                        <div style='margin-top: 20px;'>
                                            <a href='" + rsvpYesPlusOneUrl + @"' class='btn-rsvp btn-plus-one'>✓ Yes + Guest</a>
                                            <p class='plus-one-text'>Bringing a plus one? Click the button above!</p>
                                        </div>" : "") + @"
                                    </div>

                                    <div class='deadline-warning'>
                                        <p>⚠️ Please RSVP by <strong>" + rsvpDeadlineFormatted + @"</strong></p>
                                    </div>

                                    <p style='color: #6C757D; font-size: 14px; text-align: center; margin: 24px 0 0 0;'>
                                        Can't click the buttons? Visit our website and login to RSVP:
                                        <a href='" + frontendBaseUrl + @"/events' style='color: #4263EB;'>" + frontendBaseUrl + @"/events</a>
                                    </p>
                                </div>
                                <div class='footer'>
                                    <p><strong>Birmingham Committee on Foreign Relations</strong></p>
                                    <p style='margin:4px 0;'>Birmingham, AL</p>
                                    <p>© 2025 BCFR. All rights reserved.</p>
                                    <p>You received this email as a member of BCFR</p>
                                    <p><a href='" + frontendBaseUrl + @"'>Visit Our Website</a> | <a href='mailto:info@birminghamforeignrelations.org'>Contact Us</a></p>
                                    <p style='margin-top:12px;font-size:12px;color:#9CA3AF;'>To unsubscribe, reply to this email with &quot;Unsubscribe&quot; in the subject line.</p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                var textBody = $@"New Event Announcement

Dear {firstName},

We're excited to invite you to our upcoming event!

EVENT DETAILS
=============
{eventTitle}

{eventDescription}

Date: {eventDateFormatted}
Time: {startTimeFormatted} - {endTimeFormatted}
Location: {location}
Featured Speaker: {speaker}

RSVP
====
Please RSVP by {rsvpDeadlineFormatted}

To RSVP, please visit: {frontendBaseUrl}/events

Best regards,
The BCFR Team

Birmingham Committee on Foreign Relations
Birmingham, AL
© 2025 BCFR. All rights reserved.

To unsubscribe, reply to this email with ""Unsubscribe"" in the subject line.";

                var unsubscribeEmail = _configuration["Resend:UnsubscribeEmail"] ?? "admin@birminghamforeignrelations.org";

                var message = new EmailMessage
                {
                    From = _fromName + " <" + _fromEmail + ">",
                    To = toEmail,
                    Subject = "BCFR Event Invitation: " + eventTitle,
                    HtmlBody = htmlBody,
                    TextBody = textBody,
                    Headers = new Dictionary<string, string>
                    {
                        { "List-Unsubscribe", $"<mailto:{unsubscribeEmail}?subject=Unsubscribe>" }
                    }
                };

                await _resend.EmailSendAsync(message);
                _logger.LogInformation($"Event announcement email sent to {toEmail} for event: {eventTitle}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send event announcement email to {toEmail}");
                return false;
            }
        }

    }
}
