using MemberOrgApi.Models;

namespace MemberOrgApi.Services;

public interface IActivityLogService
{
    Task LogActivityAsync(
        Guid userId,
        string activityType,
        string activityCategory,
        string description,
        object? oldValue = null,
        object? newValue = null,
        Guid? performedById = null,
        Dictionary<string, object>? metadata = null);
    
    Task LogLoginAsync(Guid userId, string ipAddress, string userAgent, bool success = true);
    Task LogProfileUpdateAsync(Guid userId, string fieldName, object? oldValue, object? newValue, Guid? performedById = null);
    Task LogSubscriptionChangeAsync(Guid userId, string changeType, object? details = null);
    Task LogAdminActionAsync(Guid targetUserId, Guid adminId, string action, string description);
    
    Task<IEnumerable<ActivityLog>> GetUserActivitiesAsync(Guid userId, int skip = 0, int take = 50);
    Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int skip = 0, int take = 50);
    Task<Dictionary<string, int>> GetActivityStatsAsync(Guid? userId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<ActivityLog>> SearchActivitiesAsync(
        string? activityType = null,
        string? activityCategory = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 50);
}