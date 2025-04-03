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
using API.Services;


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
        private readonly ITokenService service;

        public AccountRepository(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, 
            IConfiguration configuration, RoleManager<IdentityRole> roleManager,
            ILogger<AccountRepository> logger,
            ApplicationDbContext context,
            IMapper mapper,
            ITokenService service)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.roleManager= roleManager;
            this.logger=logger;
            this.context = context;
            this.mapper = mapper;
            this.service= service;
        }

        public async Task<bool> ClearDatabaseAsync()
        {
            logger.LogInformation("Starting to clear database data for Identity.");
            bool success = true;

            try
            {
                // First, identify admin users to preserve
                var users = await userManager.Users.ToListAsync();
                var adminUsers = new List<ApplicationUser>();

                foreach (var user in users)
                {
                    if (await userManager.IsInRoleAsync(user, AppRole.Admin))
                    {
                        adminUsers.Add(user);
                        logger.LogInformation("Identified admin user to preserve: {UserId}", user.Id);
                    }
                }

                // Delete transactions first (they depend on wallets and categories)
                logger.LogInformation("Deleting transactions...");
                await context.Transactions
                    .Where(t => !adminUsers.Select(a => a.Id).Contains(
                        context.Wallets.Where(w => w.WalletID == t.WalletID).Select(w => w.UserID).FirstOrDefault()
                    ))
                    .ExecuteDeleteAsync();

                // Delete wallets next (they depend on users)
                logger.LogInformation("Deleting wallets...");
                await context.Wallets
                    .Where(w => !adminUsers.Select(a => a.Id).Contains(w.UserID))
                    .ExecuteDeleteAsync();

                // Delete refresh tokens (they depend on users)
                logger.LogInformation("Deleting refresh tokens...");
                await context.RefreshTokens
                    .Where(rt => !adminUsers.Select(a => a.Id).Contains(rt.UserId))
                    .ExecuteDeleteAsync();

                // Now delete the users (excluding admins)
                logger.LogInformation("Deleting non-admin users...");
                foreach (var user in users)
                {
                    if (adminUsers.Contains(user))
                    {
                        logger.LogInformation("Skipping admin user: {UserId}", user.Id);
                        continue;
                    }

                    try
                    {
                        var result = await userManager.DeleteAsync(user);
                        if (!result.Succeeded)
                        {
                            logger.LogError("Failed to delete user {UserId}. Errors: {Errors}",
                                user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                            success = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Exception occurred while deleting user {UserId}", user.Id);
                        success = false;
                    }
                }

                // Finally delete non-admin roles
                logger.LogInformation("Deleting non-admin roles...");
                var roles = await roleManager.Roles
                    .Where(r => r.Name != AppRole.Admin)
                    .ToListAsync();

                foreach (var role in roles)
                {
                    try
                    {
                        var result = await roleManager.DeleteAsync(role);
                        if (!result.Succeeded)
                        {
                            logger.LogError("Failed to delete role {RoleId}. Errors: {Errors}",
                                role.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                            success = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Exception occurred while deleting role {RoleId}", role.Id);
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred during database clear operation");
                success = false;
            }

            logger.LogInformation("Database clear operation completed.");
            return success;
        }

        public async Task<AuthenticationResult> SignInAsync(SignInDTO model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "User does not exist" },
                    Success = false
                };
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "Invalid login credentials" },
                    Success = false
                };
            }

            return await service.GenerateTokensAsync(user);
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
            return await service.RefreshTokenAsync(model.ExpiredToken);
        }
        
    }

}
