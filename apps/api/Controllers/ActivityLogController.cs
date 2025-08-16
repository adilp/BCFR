using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MemberOrgApi.Services;
using MemberOrgApi.Models;
using System.Security.Claims;

namespace MemberOrgApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ActivityLogController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<ActivityLogController> _logger;

    public ActivityLogController(
        IActivityLogService activityLogService,
        ILogger<ActivityLogController> logger)
    {
        _activityLogService = activityLogService;
        _logger = logger;
    }

    [HttpGet("user/{userId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<ActivityLog>>> GetUserActivities(
        Guid userId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? activityCategory = null)
    {
        try
        {
            // If category filter is specified, use search method instead
            if (!string.IsNullOrEmpty(activityCategory))
            {
                var filteredActivities = await _activityLogService.SearchActivitiesAsync(
                    activityType: null,
                    activityCategory: activityCategory,
                    userId: userId,
                    startDate: null,
                    endDate: null,
                    skip: skip,
                    take: take);
                return Ok(filteredActivities);
            }
            
            var activities = await _activityLogService.GetUserActivitiesAsync(userId, skip, take);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activities for user {UserId}", userId);
            return StatusCode(500, new { message = "Error fetching user activities" });
        }
    }

    [HttpGet("my-activities")]
    public async Task<ActionResult<IEnumerable<ActivityLog>>> GetMyActivities(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? activityCategory = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            // If category filter is specified, use search method instead
            if (!string.IsNullOrEmpty(activityCategory))
            {
                var filteredActivities = await _activityLogService.SearchActivitiesAsync(
                    activityType: null,
                    activityCategory: activityCategory,
                    userId: userId,
                    startDate: null,
                    endDate: null,
                    skip: skip,
                    take: take);
                return Ok(filteredActivities);
            }

            var activities = await _activityLogService.GetUserActivitiesAsync(userId, skip, take);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current user activities");
            return StatusCode(500, new { message = "Error fetching activities" });
        }
    }

    [HttpGet("recent")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<ActivityLog>>> GetRecentActivities(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var activities = await _activityLogService.GetRecentActivitiesAsync(skip, take);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activities");
            return StatusCode(500, new { message = "Error fetching recent activities" });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<Dictionary<string, int>>> GetActivityStats(
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Non-admins can only see their own stats
            if (!User.IsInRole("Admin") && userId.HasValue)
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var parsedUserId) || parsedUserId != userId)
                {
                    return Forbid();
                }
            }

            // If no userId specified and not admin, use current user
            if (!userId.HasValue && !User.IsInRole("Admin"))
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var parsedUserId))
                {
                    return Unauthorized();
                }
                userId = parsedUserId;
            }

            var stats = await _activityLogService.GetActivityStatsAsync(userId, startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activity stats");
            return StatusCode(500, new { message = "Error fetching activity stats" });
        }
    }

    [HttpGet("search")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<ActivityLog>>> SearchActivities(
        [FromQuery] string? activityType = null,
        [FromQuery] string? activityCategory = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var activities = await _activityLogService.SearchActivitiesAsync(
                activityType, 
                activityCategory, 
                userId, 
                startDate, 
                endDate, 
                skip, 
                take);
            
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching activities");
            return StatusCode(500, new { message = "Error searching activities" });
        }
    }
}