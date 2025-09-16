using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MemberOrgApi.Data;
using MemberOrgApi.Models;

namespace MemberOrgApi.Services
{
    public class TokenService : ITokenService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TokenService> _logger;

        public TokenService(AppDbContext context, ILogger<TokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RsvpToken> GenerateRsvpTokenAsync(Guid userId, Guid eventId, DateTime expiresAt)
        {
            // Check if token already exists for this user and event
            var existingToken = await _context.RsvpTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.EventId == eventId && t.UsedAt == null);

            if (existingToken != null)
            {
                // Return existing unused token
                _logger.LogInformation($"Returning existing RSVP token for user {userId} and event {eventId}");
                return existingToken;
            }

            // Generate new token
            var token = new RsvpToken
            {
                Token = GenerateSecureToken(),
                UserId = userId,
                EventId = eventId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            _context.RsvpTokens.Add(token);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Generated new RSVP token for user {userId} and event {eventId}");
            return token;
        }

        public async Task<RsvpToken?> ValidateRsvpTokenAsync(string token)
        {
            var rsvpToken = await _context.RsvpTokens
                .Include(t => t.User)
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (rsvpToken == null)
            {
                _logger.LogWarning($"Token not found: {token}");
                return null;
            }

            if (rsvpToken.UsedAt != null)
            {
                _logger.LogWarning($"Token already used: {token}");
                return null;
            }

            if (DateTime.UtcNow > rsvpToken.ExpiresAt)
            {
                _logger.LogWarning($"Token expired: {token}");
                return null;
            }

            return rsvpToken;
        }

        public async Task<bool> MarkTokenAsUsedAsync(string token, string response, bool? plusOne = null)
        {
            var rsvpToken = await _context.RsvpTokens.FirstOrDefaultAsync(t => t.Token == token);

            if (rsvpToken == null)
                return false;

            rsvpToken.UsedAt = DateTime.UtcNow;
            rsvpToken.UsedForResponse = response;
            rsvpToken.UsedWithPlusOne = plusOne;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Marked token {token} as used for response: {response}");
            return true;
        }

        public async Task<bool> CleanupExpiredTokensAsync()
        {
            try
            {
                var expiredTokens = await _context.RsvpTokens
                    .Where(t => t.ExpiresAt < DateTime.UtcNow && t.UsedAt == null)
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _context.RsvpTokens.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Cleaned up {expiredTokens.Count} expired tokens");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
                return false;
            }
        }

        public string GenerateSecureToken()
        {
            // Generate a cryptographically secure random token
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);

                // Convert to URL-safe base64
                var base64 = Convert.ToBase64String(bytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                // Add timestamp for uniqueness
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return $"{base64}_{timestamp}";
            }
        }
    }
}