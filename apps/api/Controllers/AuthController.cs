using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MemberOrgApi.Data;
using MemberOrgApi.DTOs;
using MemberOrgApi.Models;
using MemberOrgApi.Services;
using MemberOrgApi.Constants;
using Microsoft.AspNetCore.Authorization;
using MemberOrgApi.DTOs;
using MemberOrgApi.Constants;

namespace MemberOrgApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailService _emailService;
    private readonly IActivityLogService _activityLogService;

    public AuthController(
        AppDbContext context, 
        IConfiguration configuration, 
        ILogger<AuthController> logger, 
        IEmailService emailService,
        IActivityLogService activityLogService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _activityLogService = activityLogService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
    {
        try
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Phone = request.Phone,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Country = request.Country ?? "United States",
                DietaryRestrictions = request.DietaryRestrictions
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate token and create session
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var session = new Session
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = expiresAt,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Log registration activity
            await _activityLogService.LogActivityAsync(
                user.Id,
                ActivityTypes.Registration,
                ActivityCategories.Authentication,
                "New user registration",
                metadata: new Dictionary<string, object> 
                { 
                    { "Email", user.Email },
                    { "Username", user.Username }
                });

            // Send welcome email asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, user.LastName);
                    _logger.LogInformation($"Welcome email sent to {user.Email}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Failed to send welcome email to {user.Email}");
                }
            });

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            // Allow login by username or email
            var loginId = (request.Username ?? string.Empty).Trim();
            User? user = null;
            if (loginId.Contains('@'))
            {
                var loginLower = loginId.ToLower();
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == loginLower);
            }
            else
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginId);
            }
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Log failed login attempt if user exists
                if (user != null)
                {
                    await _activityLogService.LogLoginAsync(
                        user.Id, 
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        Request.Headers["User-Agent"].ToString(),
                        success: false);
                }
                return Unauthorized(new { message = "Invalid username or password" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Account is disabled" });
            }

            // Generate token and create session
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var session = new Session
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = expiresAt,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Log successful login
            await _activityLogService.LogLoginAsync(
                user.Id,
                session.IpAddress ?? "Unknown",
                session.UserAgent ?? "Unknown",
                success: true);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var id = (request.EmailOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id))
                return Ok(new { message = "If an account exists, a reset email has been sent." });

            User? user = null;
            if (id.Contains('@'))
            {
                var idLower = id.ToLower();
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == idLower);
            }
            else
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == id);
            }

            if (user != null)
            {
                // Generate reset token (1 hour expiry)
                var tokenService = HttpContext.RequestServices.GetRequiredService<ITokenService>();
                var tokenString = tokenService.GenerateSecureToken();

                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = tokenString,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    RequestedIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                await _emailService.SendPasswordResetEmailAsync(user.Email, tokenString);

                // Optional: log password reset request
                await _activityLogService.LogActivityAsync(
                    user.Id,
                    ActivityTypes.PasswordReset,
                    ActivityCategories.Authentication,
                    "Password reset requested");
            }

            // Always return OK to prevent account enumeration
            return Ok(new { message = "If an account exists, a reset email has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot-password");
            // Still return OK to avoid enumeration
            return Ok(new { message = "If an account exists, a reset email has been sent." });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { message = "Invalid request" });
            }

            var token = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            if (token == null || token.UsedAt != null || token.ExpiresAt < DateTime.UtcNow || token.User == null)
            {
                return BadRequest(new { message = "Invalid or expired token" });
            }

            // Update password
            token.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            token.User.UpdatedAt = DateTime.UtcNow;
            token.UsedAt = DateTime.UtcNow;

            // Invalidate sessions for this user
            var sessions = _context.Sessions.Where(s => s.UserId == token.UserId);
            _context.Sessions.RemoveRange(sessions);

            await _context.SaveChangesAsync();

            // Log activity
            await _activityLogService.LogActivityAsync(
                token.UserId,
                ActivityTypes.PasswordChange,
                ActivityCategories.Authentication,
                "Password reset completed");

            return Ok(new { message = "Password has been reset" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reset-password");
            return StatusCode(500, new { message = "An error occurred during password reset" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Token == token);

            if (session != null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                DateOfBirth = user.DateOfBirth
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "ThisIsAVerySecureKeyForDevelopment123456789012"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "MemberOrgApi",
            audience: _configuration["Jwt:Audience"] ?? "MemberOrgApp",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
