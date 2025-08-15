using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;
using MemberOrgApi.Models;
using MemberOrgApi.Constants;
using System.Text.Json;

namespace MemberOrgApi.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ActivityLogService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogActivityAsync(
        Guid userId,
        string activityType,
        string activityCategory,
        string description,
        object? oldValue = null,
        object? newValue = null,
        Guid? performedById = null,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var activity = new ActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                ActivityCategory = activityCategory,
                Description = description,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                PerformedById = performedById,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(activity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log activity for user {UserId}", userId);
        }
    }

    public async Task LogLoginAsync(Guid userId, string ipAddress, string userAgent, bool success = true)
    {
        await LogActivityAsync(
            userId,
            success ? ActivityTypes.Login : ActivityTypes.LoginFailed,
            ActivityCategories.Authentication,
            success ? "User logged in successfully" : "Login attempt failed",
            metadata: new Dictionary<string, object> 
            { 
                { "IpAddress", ipAddress },
                { "UserAgent", userAgent },
                { "Success", success }
            });
    }

    public async Task LogProfileUpdateAsync(Guid userId, string fieldName, object? oldValue, object? newValue, Guid? performedById = null)
    {
        var description = performedById.HasValue && performedById != userId
            ? $"Profile field '{fieldName}' was updated by an administrator"
            : $"Profile field '{fieldName}' was updated";

        await LogActivityAsync(
            userId,
            ActivityTypes.ProfileUpdate,
            ActivityCategories.Profile,
            description,
            oldValue,
            newValue,
            performedById);
    }

    public async Task LogSubscriptionChangeAsync(Guid userId, string changeType, object? details = null)
    {
        var activityType = changeType switch
        {
            "created" => ActivityTypes.SubscriptionCreated,
            "updated" => ActivityTypes.SubscriptionUpdated,
            "canceled" => ActivityTypes.SubscriptionCanceled,
            _ => ActivityTypes.SubscriptionUpdated
        };

        await LogActivityAsync(
            userId,
            activityType,
            ActivityCategories.Subscription,
            $"Subscription {changeType}",
            newValue: details);
    }

    public async Task LogAdminActionAsync(Guid targetUserId, Guid adminId, string action, string description)
    {
        await LogActivityAsync(
            targetUserId,
            action,
            ActivityCategories.Administration,
            description,
            performedById: adminId);
    }

    public async Task<IEnumerable<ActivityLog>> GetUserActivitiesAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _context.ActivityLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(a => a.PerformedBy)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int skip = 0, int take = 50)
    {
        return await _context.ActivityLogs
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(a => a.User)
            .Include(a => a.PerformedBy)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetActivityStatsAsync(Guid? userId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.ActivityLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);
        
        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt <= endDate.Value);

        var stats = await query
            .GroupBy(a => a.ActivityCategory)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);

        return stats;
    }

    public async Task<IEnumerable<ActivityLog>> SearchActivitiesAsync(
        string? activityType = null,
        string? activityCategory = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 50)
    {
        var query = _context.ActivityLogs.AsQueryable();

        if (!string.IsNullOrEmpty(activityType))
            query = query.Where(a => a.ActivityType == activityType);

        if (!string.IsNullOrEmpty(activityCategory))
            query = query.Where(a => a.ActivityCategory == activityCategory);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(a => a.User)
            .Include(a => a.PerformedBy)
            .ToListAsync();
    }
}