using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;
using MemberOrgApi.Models;
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
        _logger.LogInformation("GetProfile called. User claims: {Claims}", 
            string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Failed to parse user ID from claim: {Claim}", userIdClaim);
            return Unauthorized();
        }

        _logger.LogInformation("Looking for user with ID: {UserId}", userId);

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var profileDto = new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Phone = user.Phone,
            Address = user.Address,
            City = user.City,
            State = user.State,
            ZipCode = user.ZipCode,
            Country = user.Country,
            CreatedAt = user.CreatedAt
        };

        return Ok(profileDto);
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var subscription = await _context.MembershipSubscriptions
            .Where(s => s.UserId == userId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return NotFound(new { message = "No active subscription found" });
        }

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
}