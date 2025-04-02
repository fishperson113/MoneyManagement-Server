using API.Models.DTOs;
using API.Models.Entities;
using System.Security.Claims;
using API.Helpers;
namespace API.Services
{
    public interface ITokenService
    {
        Task<AuthenticationResult> GenerateTokensAsync(ApplicationUser user);
        Task<AuthenticationResult> RefreshTokenAsync(string expiredToken);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    }
}
