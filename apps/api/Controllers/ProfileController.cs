using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;
using MemberOrgApi.Models;
using MemberOrgApi.DTOs;
using System.Security.Claims;

namespace MemberOrgApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(AppDbContext context, ILogger<ProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
        return Ok(new { message = "Profile controller is working!" });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            _logger.LogInformation("GetProfile called");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("GetProfile failed - Invalid user ID in claims: {Claim}", userIdClaim);
                return Unauthorized();
            }

            _logger.LogInformation("Retrieving profile for UserId: {UserId}", userId);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("GetProfile failed - User not found: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            var profileDto = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                DateOfBirth = user.DateOfBirth,
                Phone = user.Phone,
                Address = user.Address,
                City = user.City,
                State = user.State,
                ZipCode = user.ZipCode,
                Country = user.Country,
                DietaryRestrictions = user.DietaryRestrictions,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            _logger.LogInformation("Successfully retrieved profile for UserId: {UserId}, Email: {Email}",
                userId, user.Email);
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile");
            return StatusCode(500, new { message = "An error occurred while retrieving your profile" });
        }
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Check if email is being changed and if it's already taken
        if (user.Email != updateDto.Email)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == updateDto.Email && u.Id != userId);
            if (emailExists)
            {
                return BadRequest(new { message = "Email is already taken" });
            }
        }

        // Check if username is being changed and if it's already taken
        if (user.Username != updateDto.Username)
        {
            var usernameExists = await _context.Users.AnyAsync(u => u.Username == updateDto.Username && u.Id != userId);
            if (usernameExists)
            {
                return BadRequest(new { message = "Username is already taken" });
            }
        }

        // Update user fields
        user.FirstName = updateDto.FirstName;
        user.LastName = updateDto.LastName;
        user.Email = updateDto.Email;
        user.Username = updateDto.Username;
        user.DateOfBirth = updateDto.DateOfBirth;
        user.Phone = updateDto.Phone;
        user.Address = updateDto.Address;
        user.City = updateDto.City;
        user.State = updateDto.State;
        user.ZipCode = updateDto.ZipCode;
        user.Country = updateDto.Country;
        user.DietaryRestrictions = updateDto.DietaryRestrictions;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} updated their profile", userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while updating the profile" });
        }
    }

    [HttpGet("subscription")]
    [Authorize]
    public async Task<IActionResult> GetSubscription()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("GetSubscription called with invalid user ID in claims: {Claim}", userIdClaim);
                return Unauthorized();
            }

            _logger.LogInformation("Retrieving subscription for UserId: {UserId}", userId);

            var subscription = await _context.MembershipSubscriptions
                .Where(s => s.UserId == userId && s.Status == "active")
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                _logger.LogInformation("No active subscription found for UserId: {UserId}", userId);
                return NotFound(new { message = "No active subscription found" });
            }

            _logger.LogInformation("Retrieved subscription for UserId: {UserId}, Tier: {Tier}, Status: {Status}",
                userId, subscription.MembershipTier, subscription.Status);

            return Ok(new
            {
                id = subscription.StripeSubscriptionId,
                status = subscription.Status,
                membershipTier = subscription.MembershipTier,
                amount = subscription.Amount,
                startDate = subscription.StartDate,
                endDate = subscription.EndDate,
                nextBillingDate = subscription.NextBillingDate,
                createdAt = subscription.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription");
            return StatusCode(500, new { message = "An error occurred while retrieving your subscription" });
        }
    }
}