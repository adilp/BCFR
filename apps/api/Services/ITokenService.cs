using System;
using System.Threading.Tasks;
using MemberOrgApi.Models;

namespace MemberOrgApi.Services
{
    public interface ITokenService
    {
        Task<RsvpToken> GenerateRsvpTokenAsync(Guid userId, Guid eventId, DateTime expiresAt);
        Task<RsvpToken?> ValidateRsvpTokenAsync(string token);
        Task<bool> MarkTokenAsUsedAsync(string token, string response, bool? plusOne = null);
        Task<bool> CleanupExpiredTokensAsync();
        string GenerateSecureToken();
    }
}