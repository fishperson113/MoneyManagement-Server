using API.Data;
using API.Helpers;
using API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class SeedService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public SeedService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
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
    public async Task SeedTestUserWithData()
    {
        // Ensure the role exists
        if (!await _roleManager.RoleExistsAsync(AppRole.Customer))
        {
            await _roleManager.CreateAsync(new IdentityRole(AppRole.Customer));
        }
        // Create test user
        var testUserEmail = "test@example.com";
        var testUser = await _userManager.FindByEmailAsync(testUserEmail);

        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = testUserEmail,
                Email = testUserEmail,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "User"
            };

            var result = await _userManager.CreateAsync(testUser, "Test@123");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(testUser, AppRole.Customer);
            }
            else
            {
                // Log or handle the user creation error
                throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Check if test user already has wallets
        if (!await _dbContext.Wallets.AnyAsync(w => w.UserID == testUser.Id))
        {
            // Add wallets
            var wallets = new List<Wallet>
            {
                new Wallet
                {
                    WalletID = Guid.NewGuid(),
                    WalletName = "Cash Wallet",
                    Balance = 1000000M,
                    UserID = testUser.Id,
                    User = testUser
                },
                new Wallet
                {
                    WalletID = Guid.NewGuid(),
                    WalletName = "Bank Account",
                    Balance = 5000000M,
                    UserID = testUser.Id,
                    User = testUser
                },
                new Wallet
                {
                    WalletID = Guid.NewGuid(),
                    WalletName = "Credit Card",
                    Balance = -2000000M,
                    UserID = testUser.Id,
                    User = testUser
                }
            };

            await _dbContext.Wallets.AddRangeAsync(wallets);
            await _dbContext.SaveChangesAsync();

            // Add categories
            var now = DateTime.Now;
            var expenseCategories = new List<Category>
            {
                new Category { CategoryID = Guid.NewGuid(), Name = "Food & Dining", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Transportation", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Entertainment", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Housing", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Utilities", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Shopping", CreatedAt = now }
            };

            var incomeCategories = new List<Category>
            {
                new Category { CategoryID = Guid.NewGuid(), Name = "Salary", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Freelance", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Gifts", CreatedAt = now },
                new Category { CategoryID = Guid.NewGuid(), Name = "Investments", CreatedAt = now }
            };

            await _dbContext.Categories.AddRangeAsync(expenseCategories);
            await _dbContext.Categories.AddRangeAsync(incomeCategories);
            await _dbContext.SaveChangesAsync();

            // Add transactions
            var allCategories = expenseCategories.Concat(incomeCategories).ToList();
            var random = new Random();
            var transactions = new List<Transaction>();

            // Generate random transactions for each wallet
            foreach (var wallet in wallets)
            {
                // Generate 5-10 transactions per wallet over the past 30 days
                int transactionCount = random.Next(5, 11);
                for (int i = 0; i < transactionCount; i++)
                {
                    var category = allCategories[random.Next(allCategories.Count)];
                    var isExpense = expenseCategories.Contains(category);

                    // Generate amounts: negative for expenses, positive for income
                    var amount = isExpense
                        ? random.Next(50000, 500000) * -1M
                        : random.Next(100000, 1000000) * 1M;

                    var daysAgo = random.Next(0, 30);
                    var date = DateTime.Now.AddDays(-daysAgo);

                    transactions.Add(new Transaction
                    {
                        TransactionID = Guid.NewGuid(),
                        Amount = amount,
                        Description = $"{category.Name} transaction",
                        TransactionDate = date,
                        WalletID = wallet.WalletID,
                        Wallet = wallet,
                        CategoryID = category.CategoryID,
                        Category = category
                    });
                }
            }

            await _dbContext.Transactions.AddRangeAsync(transactions);
            await _dbContext.SaveChangesAsync();
        }
    }
}

