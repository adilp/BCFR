using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MemberOrgApi.Data;
using MemberOrgApi.DTOs;
using MemberOrgApi.Models;
using MemberOrgApi.Constants;

namespace MemberOrgApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
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

            return Ok(new UserAdminResponse
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
            });
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

            return Ok(new UserAdminResponse
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
            });
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
    public async Task<ActionResult<AdminStats>> GetStats()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var adminCount = await _context.Users.CountAsync(u => u.Role == Roles.Admin);
            var memberCount = await _context.Users.CountAsync(u => u.Role == Roles.Member);
            var activeSubscriptions = await _context.MembershipSubscriptions
                .CountAsync(s => s.Status == "active");

            return Ok(new AdminStats
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                AdminCount = adminCount,
                MemberCount = memberCount,
                ActiveSubscriptions = activeSubscriptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin stats");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}

// DTOs for Admin operations
public class UserAdminResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string>? DietaryRestrictions { get; set; }
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public List<string>? DietaryRestrictions { get; set; }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class AdminStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminCount { get; set; }
    public int MemberCount { get; set; }
    public int ActiveSubscriptions { get; set; }
}