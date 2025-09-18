using MemberOrgApi.Models;

namespace MemberOrgApi.Services;

public interface IEmailQueueService
{
    Task<Guid> QueueSingleEmailAsync(string to, string subject, string htmlBody, string? plainText = null, int priority = 1, Guid? campaignId = null, string? recipientName = null);
    Task<List<EmailQueueItem>> GetQueuedEmailsAsync(int take = 100, string? status = null);
    Task<EmailQueueItem?> GetEmailByIdAsync(Guid id);
    Task<Guid> QueueCampaignAsync(string campaignName, string campaignType, List<EmailRecipient> recipients, string subject, string htmlBody);
}
