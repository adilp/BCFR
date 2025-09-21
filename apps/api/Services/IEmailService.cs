using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemberOrgApi.Services
{
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName, string lastName);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
        Task<bool> SendMembershipConfirmationAsync(string toEmail, string membershipType, string transactionId);
        Task<bool> SendEventRegistrationConfirmationAsync(string toEmail, string eventName, DateTime eventDate);
        Task<bool> SendCustomEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null, List<EmailAttachment>? attachments = null);
        Task<bool> SendBroadcastEmailAsync(List<string> toEmails, string subject, string bodyContent, bool isHtml = true);
        Task<bool> SendEventAnnouncementEmailAsync(string toEmail, string firstName, string eventTitle,
            string eventDescription, DateTime eventDate, TimeSpan startTime, TimeSpan endTime,
            string location, string speaker, DateTime rsvpDeadline, bool allowPlusOne, string rsvpToken);
    }
}
