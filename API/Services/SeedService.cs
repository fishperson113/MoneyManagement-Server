using API.Helpers;
using API.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

public class SeedService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public SeedService(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAdminUserAndRole()
    {
        // Ensure the admin role exists FIRST
        if (!await _roleManager.RoleExistsAsync(AppRole.Admin))
        {
            await _roleManager.CreateAsync(new IdentityRole(AppRole.Admin));
        }

        // THEN ensure the admin user exists
        var adminUser = await _userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, AppRole.Admin);
            }
            else
            {
                // Log or handle the user creation error
                throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
