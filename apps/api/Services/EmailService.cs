using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using Microsoft.Extensions.Options;
using System.Net.Http;

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

                var response = await _resend.EmailSendAsync(message);
                _logger.LogInformation("Welcome email sent successfully to " + toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to " + toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            try
            {
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

        public async Task<bool> SendCustomEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            try
            {
                var message = new EmailMessage
                {
                    From = _fromName + " <" + _fromEmail + ">",
                    To = toEmail,
                    Subject = subject,
                    HtmlBody = htmlBody,
                    TextBody = textBody
                };

                await _resend.EmailSendAsync(message);
                _logger.LogInformation("Custom email sent to " + toEmail + " with subject: " + subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send custom email to " + toEmail);
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
                                    <p>© 2025 BCFR. All rights reserved.</p>
                                    <p>You received this email as a member of BCFR.</p>
                                    <p><a href='" + _configuration["App:BaseUrl"] + @"'>Visit Our Website</a></p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                var textBody = System.Text.RegularExpressions.Regex.Replace(bodyContent, "<.*?>", string.Empty);
                textBody = "Birmingham Committee on Foreign Relations\n\n" + textBody + "\n\n© 2025 BCFR. All rights reserved.";

                // Send individual emails to each recipient to maintain privacy
                foreach (var toEmail in toEmails)
                {
                    var message = new EmailMessage
                    {
                        From = _fromName + " <" + _fromEmail + ">",
                        To = toEmail,
                        Subject = subject,
                        HtmlBody = isHtml ? wrappedHtmlBody : null,
                        TextBody = !isHtml ? bodyContent : textBody
                    };

                    await _resend.EmailSendAsync(message);
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
    }
}