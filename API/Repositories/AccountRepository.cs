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
using Microsoft.AspNetCore.Mvc;


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
        private readonly FirebaseHelper firebaseHelper;
        private readonly IUserProfileMediator _profileMediator;
        public AccountRepository(
             UserManager<ApplicationUser> userManager,
             SignInManager<ApplicationUser> signInManager,
             IConfiguration configuration,
             RoleManager<IdentityRole> roleManager,
             ILogger<AccountRepository> logger,
             ApplicationDbContext context,
             IMapper mapper,
             ITokenService service,
             FirebaseHelper firebaseHelper,
             IUserProfileMediator profileMediator)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.roleManager = roleManager;
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
            this.service = service;
            this.firebaseHelper = firebaseHelper;
            _profileMediator = profileMediator;
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
                if (context.Transactions != null)
                {
                    var allTransactions = await context.Transactions.ToListAsync();
                    context.Transactions.RemoveRange(allTransactions);
                    await context.SaveChangesAsync();
                }

                // Delete wallets next (they depend on users)
                logger.LogInformation("Deleting wallets...");
                if (context.Wallets != null)
                {
                    var allWallets = await context.Wallets.ToListAsync();
                    context.Wallets.RemoveRange(allWallets);
                    await context.SaveChangesAsync();
                }
                // Delete categories (now that transactions are gone)
                logger.LogInformation("Deleting categories...");
                if (context.Categories != null)
                {
                    var allCategories = await context.Categories.ToListAsync();
                    context.Categories.RemoveRange(allCategories);
                    await context.SaveChangesAsync();
                }
                // Delete refresh tokens (they depend on users)
                logger.LogInformation("Deleting refresh tokens...");
                if (context.RefreshTokens != null)
                {
                    var refreshTokensToDelete = await context.RefreshTokens
                        .Where(rt => !adminUsers.Select(a => a.Id).Contains(rt.UserId))
                        .ToListAsync();

                    context.RefreshTokens.RemoveRange(refreshTokensToDelete);
                    await context.SaveChangesAsync();
                }

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
        public async Task<AvatarDTO> UploadAvatarAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file uploaded");
            }

            // Verify file is an image
            if (!file.ContentType.StartsWith("image/"))
            {
                throw new ArgumentException("Only image files are allowed");
            }

            // Find user
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            try
            {
                // Upload file to Firebase Storage
                var avatarUrl = await firebaseHelper.UploadUserAvatarAsync(userId, file);

                // Update user record in database
                user.AvatarUrl = avatarUrl;
                await userManager.UpdateAsync(user);
                await _profileMediator.NotifyAvatarChanged(userId, avatarUrl);

                return new AvatarDTO { AvatarUrl = avatarUrl };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
                throw; // Re-throw to be handled by controller
            }
        }
        public async Task<UserProfileDTO> GetUserProfileAsync(ClaimsPrincipal userPrincipal)
        {
            if (userPrincipal == null || !userPrincipal.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            // Lấy userId từ JWT claims
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Invalid token: no user ID found.");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return new UserProfileDTO
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = $"{user.FirstName} {user.LastName}",
                AvatarUrl = user.AvatarUrl
            };
        }
        public async Task<UserProfileDTO> GetUserByIdAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return new UserProfileDTO
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = $"{user.FirstName} {user.LastName}",
                AvatarUrl = user.AvatarUrl
            };
        }
        public async Task<UserProfileDTO> UpdateUserAsync(string userId, UpdateUserDTO model, IFormFile? avatarFile = null)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            // ✅ Require current password for any update
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                throw new InvalidOperationException("Current password is required to update profile.");

            var passwordCheck = await userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!passwordCheck)
                throw new InvalidOperationException("Current password is incorrect.");

            bool profileUpdated = false;

            // Optional: Name
            if (!string.IsNullOrWhiteSpace(model.FirstName) && model.FirstName != user.FirstName)
            {
                user.FirstName = model.FirstName;
                profileUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(model.LastName) && model.LastName != user.LastName)
            {
                user.LastName = model.LastName;
                profileUpdated = true;
            }

            // Optional: Password change
            bool wantsPasswordChange =
                !string.IsNullOrWhiteSpace(model.NewPassword) ||
                !string.IsNullOrWhiteSpace(model.ConfirmNewPassword);

            if (wantsPasswordChange)
            {
                if (string.IsNullOrWhiteSpace(model.NewPassword) ||
                    string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
                {
                    throw new InvalidOperationException("To change password, new password and confirmation are required.");
                }

                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    throw new InvalidOperationException("New password and confirmation do not match.");
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to update password: {errors}");
                }

                logger.LogInformation("Password changed for user {UserId}", userId);
            }

            // Optional: Avatar
            if (avatarFile != null)
            {
                await UploadAvatarAsync(userId, avatarFile);
            }

            if (profileUpdated)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to update profile: {errors}");
                }
            }

            // Return fresh profile
            user = await userManager.FindByIdAsync(userId);

            return new UserProfileDTO
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = $"{user.FirstName} {user.LastName}",
                AvatarUrl = user.AvatarUrl
            };
        }


    }

}
