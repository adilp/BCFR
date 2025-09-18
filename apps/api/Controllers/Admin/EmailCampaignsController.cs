using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;

namespace MemberOrgApi.Controllers.Admin;

[ApiController]
[Route("admin/emails/campaigns")]
[Authorize(Policy = "AdminOnly")]
public class EmailCampaignsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmailCampaignsController> _logger;

    public EmailCampaignsController(AppDbContext db, ILogger<EmailCampaignsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /api/admin/emails/campaigns
    [HttpGet]
    public async Task<IActionResult> ListCampaigns()
    {
        // Fetch campaigns with derived stats
        var campaigns = await _db.EmailCampaigns
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Type,
                c.Status,
                c.TotalRecipients,
                c.CreatedAt,
                c.CompletedAt,
                Stats = new
                {
                    Sent = _db.EmailQueue.Count(e => e.CampaignId == c.Id && e.Status == "Sent"),
                    Failed = _db.EmailQueue.Count(e => e.CampaignId == c.Id && e.Status == "Failed"),
                    Pending = _db.EmailQueue.Count(e => e.CampaignId == c.Id && (e.Status == "Pending" || e.Status == "Scheduled"))
                }
            })
            .ToListAsync();

        return Ok(campaigns);
    }

    // GET /api/admin/emails/campaigns/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCampaign(Guid id)
    {
        var campaign = await _db.EmailCampaigns.FirstOrDefaultAsync(c => c.Id == id);
        if (campaign == null) return NotFound();

        var emails = await _db.EmailQueue
            .Where(e => e.CampaignId == id)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.RecipientEmail,
                e.RecipientName,
                e.Subject,
                e.Status,
                e.SentAt,
                e.FailedAt,
                e.ErrorMessage
            })
            .ToListAsync();

        var response = new
        {
            Campaign = campaign,
            Stats = new
            {
                Sent = emails.Count(e => e.Status == "Sent"),
                Failed = emails.Count(e => e.Status == "Failed"),
                Pending = emails.Count(e => e.Status == "Pending" || e.Status == "Scheduled"),
                Total = emails.Count
            },
            Emails = emails
        };

        return Ok(response);
    }
}

