using API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using API.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using Microsoft.EntityFrameworkCore;
namespace API.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TokenService(IOptions<JwtSettings> jwtSettings, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _jwtSettings = jwtSettings.Value;
            _userManager = userManager;
            _context = context;
        }

        public async Task<AuthenticationResult> GenerateTokensAsync(ApplicationUser user)
        {
            var jwtToken = await CreateJwtTokenAsync(user);
            var refreshToken = await CreateRefreshTokenAsync(jwtToken, user);

            return new AuthenticationResult
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                Success = true
            };
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string expiredToken)
        {
            var principal = GetPrincipalFromExpiredToken(expiredToken);
            if (principal == null)
            {
                return new AuthenticationResult { Errors = new[] { "Invalid token" }, Success = false };
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return new AuthenticationResult { Errors = new[] { "Invalid token claims" }, Success = false };
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthenticationResult { Errors = new[] { "User not found" }, Success = false };
            }

            // Extract token identifier
            var jti = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
            {
                return new AuthenticationResult { Errors = new[] { "Invalid token claims" }, Success = false };
            }

            // Find the most recent valid refresh token for this user and token
            var refreshToken = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.JwtId == jti && !rt.Invalidated)
                .OrderByDescending(rt => rt.CreationDate)
                .FirstOrDefaultAsync();

            if (refreshToken == null)
            {
                return new AuthenticationResult { Errors = new[] { "No valid refresh token found" }, Success = false };
            }

            if (refreshToken.ExpiryDate < DateTime.UtcNow)
            {
                return new AuthenticationResult { Errors = new[] { "Refresh token has expired" }, Success = false };
            }

            // Issue a new JWT token and refresh token
            return await GenerateTokensAsync(user);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Important to set this to false
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.ValidIssuer,
                ValidAudience = _jwtSettings.ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private async Task<JwtSecurityToken> CreateJwtTokenAsync(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _jwtSettings.ValidIssuer,
                audience: _jwtSettings.ValidAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: signingCredentials
            );
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(JwtSecurityToken jwtToken, ApplicationUser user)
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                JwtId = jwtToken.Id,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Invalidated = false
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (storedRefreshToken == null)
            {
                return false;
            }

            storedRefreshToken.Invalidated = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
