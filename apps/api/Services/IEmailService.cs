using System;
using System.Threading.Tasks;

namespace MemberOrgApi.Services
{
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName, string lastName);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
        Task<bool> SendMembershipConfirmationAsync(string toEmail, string membershipType, string transactionId);
        Task<bool> SendEventRegistrationConfirmationAsync(string toEmail, string eventName, DateTime eventDate);
        Task<bool> SendCustomEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
    }
}