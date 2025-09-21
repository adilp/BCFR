using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;
using MemberOrgApi.Models;

namespace MemberOrgApi.Controllers.Admin;

[ApiController]
[Route("admin/emails/scheduled-jobs")]
[Authorize(Policy = "AdminOnly")]
public class ScheduledEmailJobsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ScheduledEmailJobsController> _logger;

    public ScheduledEmailJobsController(AppDbContext db, ILogger<ScheduledEmailJobsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /api/admin/emails/scheduled-jobs?status=Active&take=100
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status = null, [FromQuery] int take = 100)
    {
        var q = _db.ScheduledEmailJobs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(j => j.Status == status);

        var items = await q
            .OrderBy(j => j.ScheduledFor)
            .ThenBy(j => j.NextRunDate)
            .Take(Math.Clamp(take, 1, 500))
            .Select(j => new
            {
                j.Id,
                j.JobType,
                j.EntityType,
                j.EntityId,
                j.ScheduledFor,
                j.NextRunDate,
                j.LastRunDate,
                j.Status,
                j.RunCount,
                j.FailureCount,
                j.CreatedAt,
                j.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/admin/emails/scheduled-jobs/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var job = await _db.ScheduledEmailJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();
        return Ok(job);
    }

    public class RescheduleRequest
    {
        public DateTime ScheduledFor { get; set; } // Expect UTC
    }

    // POST /api/admin/emails/scheduled-jobs/{id}/reschedule
    [HttpPost("{id}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleRequest req)
    {
        var job = await _db.ScheduledEmailJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();

        job.ScheduledFor = req.ScheduledFor;
        job.NextRunDate = null;
        if (job.Status == "Completed" || job.Status == "Cancelled")
        {
            job.Status = "Active";
        }
        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Rescheduled job {JobId} to {When}", id, req.ScheduledFor);
        return Ok(new { message = "Rescheduled", job.Id, job.ScheduledFor });
    }

    // POST /api/admin/emails/scheduled-jobs/{id}/cancel
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var job = await _db.ScheduledEmailJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();

        job.Status = "Cancelled";
        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Cancelled job {JobId}", id);
        return Ok(new { message = "Cancelled", job.Id });
    }
}

