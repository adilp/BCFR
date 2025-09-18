using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;
using MemberOrgApi.Models;

namespace MemberOrgApi.Services;

public class EmailQueueService : IEmailQueueService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmailQueueService> _logger;

    public EmailQueueService(AppDbContext db, ILogger<EmailQueueService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> QueueSingleEmailAsync(string to, string subject, string htmlBody, string? plainText = null, int priority = 1, Guid? campaignId = null, string? recipientName = null)
    {
        var item = new EmailQueueItem
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            RecipientEmail = to.Trim(),
            RecipientName = recipientName,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainText,
            Status = "Pending",
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EmailQueue.Add(item);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to queue email to {Recipient}", to);
            throw;
        }

        return item.Id;
    }

    public async Task<List<EmailQueueItem>> GetQueuedEmailsAsync(int take = 100, string? status = null)
    {
        var query = _db.EmailQueue.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        return await query
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<EmailQueueItem?> GetEmailByIdAsync(Guid id)
    {
        return await _db.EmailQueue.FirstOrDefaultAsync(e => e.Id == id);
    }
}

