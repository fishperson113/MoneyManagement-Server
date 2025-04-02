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
