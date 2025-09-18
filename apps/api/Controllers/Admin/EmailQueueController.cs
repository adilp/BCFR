using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemberOrgApi.Services;

namespace MemberOrgApi.Controllers.Admin;

[ApiController]
[Route("admin/emails/queue")]
[Authorize(Policy = "AdminOnly")]
public class EmailQueueController : ControllerBase
{
    private readonly IEmailQueueService _queueService;
    private readonly ILogger<EmailQueueController> _logger;

    public EmailQueueController(IEmailQueueService queueService, ILogger<EmailQueueController> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    // GET /api/admin/emails/queue
    [HttpGet]
    public async Task<IActionResult> GetQueue([FromQuery] int take = 100, [FromQuery] string? status = null)
    {
        var items = await _queueService.GetQueuedEmailsAsync(take, status);
        return Ok(items);
    }

    // GET /api/admin/emails/queue/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmail(Guid id)
    {
        var item = await _queueService.GetEmailByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    public class QueueEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty; // HTML body
        public string? PlainText { get; set; }
        public int Priority { get; set; } = 1;
        public string? RecipientName { get; set; }
    }

    // POST /api/admin/emails/queue
    [HttpPost]
    public async Task<IActionResult> QueueEmail([FromBody] QueueEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { message = "'to', 'subject', and 'body' are required." });
        }

        var id = await _queueService.QueueSingleEmailAsync(
            request.To,
            request.Subject,
            request.Body,
            request.PlainText,
            request.Priority,
            null,
            request.RecipientName
        );

        return Ok(new { id });
    }
}
