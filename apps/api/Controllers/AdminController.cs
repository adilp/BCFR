using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MemberOrgApi.Data;
using MemberOrgApi.DTOs;
using MemberOrgApi.Models;
using MemberOrgApi.Constants;
using MemberOrgApi.Services;

namespace MemberOrgApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IActivityLogService _activityLogService;

    public AdminController(AppDbContext context, ILogger<AdminController> logger, IActivityLogService activityLogService)
    {
        _context = context;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserAdminResponse>> CreateUser(AdminCreateUserRequest request)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new { message = "Email, first name, and last name are required" });
            }

            // Enforce unique email
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return Conflict(new { message = "Email already exists" });
            }

            // Determine username
            string username = request.Username ?? (request.Email.Contains('@') ? request.Email.Split('@')[0] : request.Email);
            username = username.Trim();
            if (string.IsNullOrEmpty(username)) username = $"user{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            // Ensure unique username by appending numeric suffix if needed
            var baseUsername = username;
            int suffix = 1;
            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}{suffix}";
                suffix++;
            }

            // Generate a secure random password and hash it
            var randomPassword = Guid.NewGuid().ToString("N");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(randomPassword);

            var role = !string.IsNullOrWhiteSpace(request.Role) && Roles.IsValidRole(request.Role)
                ? request.Role
                : Roles.Member;

            var user = new User
            {
                Username = username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Phone = request.Phone,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Country = request.Country ?? "United States",
                Role = role,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Log admin action
            var currentAdminIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(currentAdminIdString, out var adminId))
            {
                await _activityLogService.LogAdminActionAsync(
                    user.Id,
                    adminId,
                    "AdminCreateUser",
                    $"Admin created user {user.Email} ({user.Username}) with role {user.Role}");
            }

            // Prepare response
            var response = new UserAdminResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                DateOfBirth = user.DateOfBirth,
                Phone = user.Phone,
                Address = user.Address,
                City = user.City,
                State = user.State,
                ZipCode = user.ZipCode,
                Country = user.Country,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DietaryRestrictions = user.DietaryRestrictions
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user by admin");
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserAdminResponse>>> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            
            var users = await query
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserAdminResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    DateOfBirth = u.DateOfBirth,
                    Phone = u.Phone,
                    Address = u.Address,
                    City = u.City,
                    State = u.State,
                    ZipCode = u.ZipCode,
                    Country = u.Country,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();
            
            // Load subscription data for each user
            var userIds = users.Select(u => u.Id).ToList();
            var subscriptions = await _context.MembershipSubscriptions
                .Where(s => userIds.Contains(s.UserId))
                .ToListAsync();
            
            // Map subscription data to users
            foreach (var user in users)
            {
                var subscription = subscriptions.FirstOrDefault(s => s.UserId == user.Id);
                if (subscription != null)
                {
                    user.MembershipTier = subscription.MembershipTier;
                    user.SubscriptionStatus = subscription.Status;
                    user.StripeCustomerId = subscription.StripeCustomerId;
                    user.NextBillingDate = subscription.NextBillingDate;
                    user.Amount = subscription.Amount;
                }
            }

            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page", page.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserAdminResponse>> GetUser(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var response = new UserAdminResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                DateOfBirth = user.DateOfBirth,
                Phone = user.Phone,
                Address = user.Address,
                City = user.City,
                State = user.State,
                ZipCode = user.ZipCode,
                Country = user.Country,
                DietaryRestrictions = user.DietaryRestrictions,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
            
            // Load subscription data
            var subscription = await _context.MembershipSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == user.Id);
            
            if (subscription != null)
            {
                response.MembershipTier = subscription.MembershipTier;
                response.SubscriptionStatus = subscription.Status;
                response.StripeCustomerId = subscription.StripeCustomerId;
                response.NextBillingDate = subscription.NextBillingDate;
                response.Amount = subscription.Amount;
            }
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPut("users/{id}")]
    public async Task<ActionResult<UserAdminResponse>> UpdateUser(Guid id, UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update user properties if provided
            if (!string.IsNullOrEmpty(request.Email))
            {
                // Check if email is already taken by another user
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                {
                    return BadRequest(new { message = "Email already exists" });
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;
            
            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;
            
            if (!string.IsNullOrEmpty(request.Role) && Roles.IsValidRole(request.Role))
                user.Role = request.Role;
            
            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;
            
            if (!string.IsNullOrEmpty(request.Phone))
                user.Phone = request.Phone;
            
            if (!string.IsNullOrEmpty(request.Address))
                user.Address = request.Address;
            
            if (!string.IsNullOrEmpty(request.City))
                user.City = request.City;
            
            if (!string.IsNullOrEmpty(request.State))
                user.State = request.State;
            
            if (!string.IsNullOrEmpty(request.ZipCode))
                user.ZipCode = request.ZipCode;
            
            if (!string.IsNullOrEmpty(request.Country))
                user.Country = request.Country;
            
            if (request.DietaryRestrictions != null)
                user.DietaryRestrictions = request.DietaryRestrictions;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new UserAdminResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                DateOfBirth = user.DateOfBirth,
                Phone = user.Phone,
                Address = user.Address,
                City = user.City,
                State = user.State,
                ZipCode = user.ZipCode,
                Country = user.Country,
                DietaryRestrictions = user.DietaryRestrictions,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
            
            // Load subscription data
            var subscription = await _context.MembershipSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == user.Id);
            
            if (subscription != null)
            {
                response.MembershipTier = subscription.MembershipTier;
                response.SubscriptionStatus = subscription.Status;
                response.StripeCustomerId = subscription.StripeCustomerId;
                response.NextBillingDate = subscription.NextBillingDate;
                response.Amount = subscription.Amount;
            }
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!Roles.IsValidRole(request.Role))
            {
                return BadRequest(new { message = "Invalid role" });
            }

            user.Role = request.Role;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Role updated successfully", role = user.Role });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role {UserId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPost("users/{id}/check-payment")]
    public async Task<ActionResult<UserAdminResponse>> RecordCheckPayment(Guid id, RecordCheckPaymentRequest request)
    {
        try
        {
            // Verify user exists
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate membership tier
            var validTiers = new[] { "over40", "under40", "student" };
            if (!validTiers.Contains(request.MembershipTier.ToLower()))
            {
                return BadRequest(new { message = "Invalid membership tier. Must be over40, under40, or student" });
            }

            // Check if user already has a subscription
            var existingSubscription = await _context.MembershipSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == id);

            if (existingSubscription != null)
            {
                // Update existing subscription
                existingSubscription.MembershipTier = request.MembershipTier.ToLower();
                existingSubscription.Amount = request.Amount;
                existingSubscription.StartDate = request.StartDate;
                existingSubscription.NextBillingDate = request.StartDate.AddYears(1);
                existingSubscription.Status = "active";
                existingSubscription.UpdatedAt = DateTime.UtcNow;
                
                // Update Stripe IDs to indicate check payment if they were Stripe payments before
                if (!existingSubscription.StripeCustomerId.StartsWith("CHECK_"))
                {
                    existingSubscription.StripeCustomerId = $"CHECK_{DateTime.UtcNow:yyyyMMddHHmmss}";
                    existingSubscription.StripeSubscriptionId = $"MANUAL_{Guid.NewGuid()}";
                }
            }
            else
            {
                // Create new subscription record for check payment
                var subscription = new MembershipSubscription
                {
                    UserId = id,
                    MembershipTier = request.MembershipTier.ToLower(),
                    StripeCustomerId = $"CHECK_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    StripeSubscriptionId = $"MANUAL_{Guid.NewGuid()}",
                    Status = "active",
                    StartDate = request.StartDate,
                    EndDate = null,
                    NextBillingDate = request.StartDate.AddYears(1),
                    Amount = request.Amount,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.MembershipSubscriptions.Add(subscription);
            }

            await _context.SaveChangesAsync();

            // Log the check payment activity
            await _activityLogService.LogActivityAsync(
                user.Id,
                ActivityTypes.PaymentSucceeded,
                ActivityCategories.Subscription,
                $"Check payment recorded: {request.MembershipTier} - ${request.Amount}",
                metadata: new Dictionary<string, object>
                {
                    { "MembershipTier", request.MembershipTier },
                    { "Amount", request.Amount },
                    { "StartDate", request.StartDate },
                    { "PaymentMethod", "Check" }
                });

            // Return the updated user with subscription data
            return await GetUser(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording check payment for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while recording the check payment" });
        }
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Don't allow deleting yourself
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
            {
                return BadRequest(new { message = "Cannot delete your own account" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStats>> GetStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? period = null) // "month", "quarter", "year", "all"
    {
        try
        {
            // Determine date range for current period
            var (periodStart, periodEnd) = GetDateRange(period, startDate, endDate);

            // Calculate previous period for growth comparisons
            var (prevStart, prevEnd) = GetPreviousPeriod(periodStart, periodEnd);
            var hasPreviousPeriod = prevStart != DateTime.MinValue || prevEnd != DateTime.MinValue;

            // Basic Counts
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);

            var newUsersThisPeriod = await _context.Users
                .CountAsync(u => u.CreatedAt >= periodStart && u.CreatedAt <= periodEnd);

            var newUsersPrevPeriod = hasPreviousPeriod
                ? await _context.Users.CountAsync(u => u.CreatedAt >= prevStart && u.CreatedAt <= prevEnd)
                : 0;

            var churnedUsersThisPeriod = await _context.MembershipSubscriptions
                .CountAsync(s => s.Status == "cancelled" &&
                                s.EndDate.HasValue &&
                                s.EndDate.Value >= periodStart &&
                                s.EndDate.Value <= periodEnd);

            // Financial Metrics
            var (mrr, arr, revenueByTier) = await CalculateRevenueStats();

            var activeSubscriptions = await _context.MembershipSubscriptions
                .CountAsync(s => s.Status == "active");

            // Calculate previous period revenue for growth rate (only if we have a valid previous period)
            var prevPeriodRevenue = hasPreviousPeriod
                ? await _context.MembershipSubscriptions
                    .Where(s => s.Status == "active" &&
                               s.StartDate >= prevStart &&
                               s.StartDate < periodStart)
                    .SumAsync(s => s.Amount)
                : 0;

            var revenueGrowthRate = CalculateGrowthRate(mrr, prevPeriodRevenue);

            var cancelledSubscriptions = await _context.MembershipSubscriptions
                .CountAsync(s => s.Status == "cancelled" &&
                               s.EndDate.HasValue &&
                               s.EndDate.Value >= periodStart);

            // Event Metrics
            var eventStats = await CalculateEventStats(periodStart, periodEnd);
            var prevEventStats = hasPreviousPeriod
                ? await CalculateEventStats(prevStart, prevEnd)
                : new EventStatsResult();
            var attendanceTrend = CalculateGrowthRate(
                eventStats.AverageAttendanceRate,
                prevEventStats.AverageAttendanceRate
            );

            // Demographics & Engagement
            var ageDistribution = await CalculateAgeDemographics();

            var totalCheckIns = await _context.EventRsvps
                .CountAsync(r => r.CheckedIn && r.Event.EventDate >= periodStart && r.Event.EventDate <= periodEnd);
            var averageEventsPerMember = activeUsers > 0 ? totalCheckIns / (decimal)activeUsers : 0;

            var topEngagedMembers = await GetTopEngagedMembers(10);

            // Period label for display
            var periodLabel = GetPeriodLabel(periodStart, periodEnd, period);

            return Ok(new AdminStats
            {
                // Basic Counts
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                NewUsersThisPeriod = newUsersThisPeriod,
                ChurnedUsersThisPeriod = churnedUsersThisPeriod,

                // Financial
                MonthlyRecurringRevenue = mrr,
                AnnualRecurringRevenue = arr,
                ActiveSubscriptions = activeSubscriptions,
                RevenueByTier = revenueByTier,
                RevenueGrowthRate = revenueGrowthRate,
                CancelledSubscriptions = cancelledSubscriptions,

                // Events
                TotalEventsHeld = eventStats.TotalEvents,
                AverageRsvpResponseRate = eventStats.AverageRsvpResponseRate,
                AverageAttendanceRate = eventStats.AverageAttendanceRate,
                AverageAttendeesPerEvent = eventStats.AverageAttendeesPerEvent,
                AttendanceTrend = attendanceTrend,

                // Demographics & Engagement
                AgeDistribution = ageDistribution,
                AverageEventsPerMember = averageEventsPerMember,
                TopEngagedMembers = topEngagedMembers,

                // Metadata
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PeriodLabel = periodLabel
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin stats");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    // Helper Methods for Stats Calculations

    private (DateTime start, DateTime end) GetDateRange(string? period, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            // Ensure provided dates are UTC
            return (
                DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc),
                DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
            );
        }

        var now = DateTime.UtcNow;

        return period?.ToLower() switch
        {
            "month" => (now.AddDays(-30), now),
            "quarter" => (DateTime.SpecifyKind(new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1), DateTimeKind.Utc), now),
            "year" => (DateTime.SpecifyKind(new DateTime(now.Year, 1, 1), DateTimeKind.Utc), now),
            _ => (DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), now) // all time
        };
    }

    private (DateTime prevStart, DateTime prevEnd) GetPreviousPeriod(DateTime start, DateTime end)
    {
        // For "All Time" (DateTime.MinValue), there is no previous period
        if (start == DateTime.MinValue)
        {
            return (
                DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)
            );
        }

        var duration = end - start;
        var prevStart = start.Add(-duration);

        // Ensure we don't go below DateTime.MinValue
        if (prevStart < DateTime.MinValue.AddDays(1))
        {
            prevStart = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        // Ensure both dates are UTC
        return (
            DateTime.SpecifyKind(prevStart, DateTimeKind.Utc),
            DateTime.SpecifyKind(start, DateTimeKind.Utc)
        );
    }

    private decimal CalculateGrowthRate(int current, int previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return ((current - previous) / (decimal)previous) * 100;
    }

    private decimal CalculateGrowthRate(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return ((current - previous) / previous) * 100;
    }

    private int CalculateAge(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }

    private async Task<Dictionary<string, int>> CalculateAgeDemographics()
    {
        var users = await _context.Users
            .Where(u => u.IsActive && u.DateOfBirth != null)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        return new Dictionary<string, int>
        {
            ["Under 30"] = users.Count(u => CalculateAge(u.DateOfBirth!.Value, today) < 30),
            ["30-40"] = users.Count(u => CalculateAge(u.DateOfBirth!.Value, today) >= 30 && CalculateAge(u.DateOfBirth!.Value, today) < 41),
            ["41-50"] = users.Count(u => CalculateAge(u.DateOfBirth!.Value, today) >= 41 && CalculateAge(u.DateOfBirth!.Value, today) < 51),
            ["51-60"] = users.Count(u => CalculateAge(u.DateOfBirth!.Value, today) >= 51 && CalculateAge(u.DateOfBirth!.Value, today) < 61),
            ["Over 60"] = users.Count(u => CalculateAge(u.DateOfBirth!.Value, today) >= 61)
        };
    }

    private async Task<(decimal mrr, decimal arr, Dictionary<string, decimal> byTier)> CalculateRevenueStats()
    {
        var activeSubscriptions = await _context.MembershipSubscriptions
            .Where(s => s.Status == "active")
            .ToListAsync();

        var mrr = activeSubscriptions.Sum(s => s.Amount);
        var arr = mrr * 12;

        var byTier = new Dictionary<string, decimal>
        {
            ["Individual"] = activeSubscriptions.Where(s => s.MembershipTier == "over40").Sum(s => s.Amount),
            ["Family"] = activeSubscriptions.Where(s => s.MembershipTier == "under40").Sum(s => s.Amount),
            ["Student"] = activeSubscriptions.Where(s => s.MembershipTier == "student").Sum(s => s.Amount)
        };

        return (mrr, arr, byTier);
    }

    private async Task<EventStatsResult> CalculateEventStats(DateTime startDate, DateTime endDate)
    {
        var events = await _context.Events
            .Include(e => e.Rsvps)
            .Where(e => e.Status == "published" && e.EventDate >= startDate && e.EventDate <= endDate)
            .ToListAsync();

        if (!events.Any())
            return new EventStatsResult();

        var totalRsvpYes = events.Sum(e => e.Rsvps.Count(r => r.Response == "yes"));
        var totalRsvpNo = events.Sum(e => e.Rsvps.Count(r => r.Response == "no"));
        var totalCheckedIn = events.Sum(e => e.Rsvps.Count(r => r.CheckedIn));
        var activeUserCount = await _context.Users.CountAsync(u => u.IsActive);

        return new EventStatsResult
        {
            TotalEvents = events.Count,
            AverageRsvpResponseRate = activeUserCount > 0 && events.Count > 0
                ? ((totalRsvpYes + totalRsvpNo) / (decimal)(activeUserCount * events.Count)) * 100
                : 0,
            AverageAttendanceRate = totalRsvpYes > 0 ? (totalCheckedIn / (decimal)totalRsvpYes) * 100 : 0,
            AverageAttendeesPerEvent = events.Count > 0 ? totalRsvpYes / (decimal)events.Count : 0
        };
    }

    private async Task<List<TopEngagedMemberDto>> GetTopEngagedMembers(int limit = 10)
    {
        var topMembers = await _context.EventRsvps
            .Where(r => r.CheckedIn)
            .GroupBy(r => r.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                EventsAttended = g.Count()
            })
            .OrderByDescending(x => x.EventsAttended)
            .Take(limit)
            .ToListAsync();

        var userIds = topMembers.Select(m => m.UserId).ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        return topMembers.Select(m => new TopEngagedMemberDto
        {
            Name = users.ContainsKey(m.UserId) ? users[m.UserId] : "Unknown",
            EventsAttended = m.EventsAttended
        }).ToList();
    }

    private string GetPeriodLabel(DateTime start, DateTime end, string? period)
    {
        if (!string.IsNullOrEmpty(period))
        {
            return period.ToLower() switch
            {
                "month" => "Last 30 Days",
                "quarter" => $"Q{(DateTime.UtcNow.Month - 1) / 3 + 1} {DateTime.UtcNow.Year}",
                "year" => $"{DateTime.UtcNow.Year}",
                _ => "All Time"
            };
        }

        if (start == DateTime.MinValue)
        {
            return "All Time";
        }

        return $"{start:MMM d, yyyy} - {end:MMM d, yyyy}";
    }

    private class EventStatsResult
    {
        public int TotalEvents { get; set; }
        public decimal AverageRsvpResponseRate { get; set; }
        public decimal AverageAttendanceRate { get; set; }
        public decimal AverageAttendeesPerEvent { get; set; }
    }
}
