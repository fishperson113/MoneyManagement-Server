using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API.Helpers;
using API.Models.Entities;
using API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Data;
using AutoMapper;


namespace API.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ILogger<AccountRepository> logger;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public AccountRepository(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, 
            IConfiguration configuration, RoleManager<IdentityRole> roleManager,
            ILogger<AccountRepository> logger,
            ApplicationDbContext context,
            IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.roleManager= roleManager;
            this.logger=logger;
            this.context = context;
            this.mapper = mapper;
        }

        public async Task<bool> ClearDatabaseAsync()
        {
            logger.LogInformation("Starting to clear database data for Identity.");

            var users = await userManager.Users.ToListAsync();
            bool success = true;
            foreach (var user in users)
            {
                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to delete user {UserId}. Errors: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    success = false;
                }
            }

            var roles = await roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var result = await roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to delete role {RoleId}. Errors: {Errors}", role.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    success = false;
                }
            }
            logger.LogInformation("Database clear operation completed.");
            return success;
        }

        public async Task<AuthenticationResult> SignInAsync(SignInDTO model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return new AuthenticationResult { Errors = new[] { "Invalid login attempt" } };
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, model.Password);

            if (!passwordValid)
            {
                return new AuthenticationResult { Errors = new[] { "Invalid login attempt" } };
            }

            return await GenerateJwtTokenAsync(user);
        }

        public async Task<IdentityResult> SignUpAsync(SignUpDTO model)
        {
            var user = mapper.Map<ApplicationUser>(model);

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                logger.LogInformation("Sign up succeeded for email: {Email}", model.Email);
                if (!await roleManager.RoleExistsAsync(AppRole.Customer))
                {
                    await roleManager.CreateAsync(new IdentityRole(AppRole.Customer));
                }
                //TODO: Send email confirmation
                //var baseUrl = configuration["AppSettings:BaseUrl"];

                //var confirmationLink = $"{baseUrl}/confirmemail?userId={user.Id}&token={Uri.EscapeDataString(token)}";

                //Console.WriteLine("Email confirmation link: " + confirmationLink);
                await userManager.AddToRoleAsync(user, AppRole.Customer);
            }
            else
            {
                logger.LogError("Sign up failed for email: {Email}. Lỗi: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            return result;
        }
        public async Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenDTO model)
        {
            var validatedToken = GetPrincipalFromToken(model.Token);

            if (validatedToken == null)
            {
                return new AuthenticationResult { Errors = new[] { "Invalid Token" } };
            }

            var expiryDateUnix = long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix);

            if (expiryDateTimeUtc > DateTime.UtcNow)
            {
                return new AuthenticationResult { Errors = new[] { "This token hasn't expired yet" } };
            }

            var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            var storedRefreshToken = await context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == model.RefreshToken);

            if (storedRefreshToken == null)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token does not exist" } };
            }

            if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has expired" } };
            }

            if (storedRefreshToken.Invalidated)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has been invalidated" } };
            }

            if (storedRefreshToken.Used)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has been used" } };
            }

            if (storedRefreshToken.JwtId != jti)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token does not match this JWT" } };
            }

            storedRefreshToken.Used = true;
            context.RefreshTokens.Update(storedRefreshToken);
            await context.SaveChangesAsync();

            var userIdClaim = validatedToken.Claims.SingleOrDefault(x => x.Type == "id");
            if (userIdClaim == null)
            {
                return new AuthenticationResult { Errors = new[] { "Invalid Token" } };
            }

            var user = await userManager.FindByIdAsync(userIdClaim.Value);
            if (user == null)
            {
                return new AuthenticationResult { Errors = new[] { "User not found" } };
            }

            return await GenerateJwtTokenAsync(user);
        }
        private ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var jwtSecret = configuration["JWT:Secret"] ?? throw new ArgumentNullException("JWT:Secret configuration is missing.");
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    ValidAudience = configuration["JWT:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateLifetime = false
                }, out var validatedToken);

                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
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
        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase);
        }
        private async Task<AuthenticationResult> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSecret = configuration["JWT:Secret"] ?? throw new ArgumentNullException("JWT:Secret configuration is missing.");
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? throw new ArgumentNullException(nameof(user.Email))),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? throw new ArgumentNullException(nameof(user.Email))),
                new Claim("id", user.Id)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            await context.RefreshTokens.AddAsync(refreshToken);
            await context.SaveChangesAsync();

            return new AuthenticationResult
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token
            };
        }
    }

}
