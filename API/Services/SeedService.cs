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

        // Check if we already have wallets in the system
        if (!await _dbContext.Wallets.AnyAsync())
        {
            // Add categories
            var now = DateTime.Now;
            var expenseCategories = new List<Category>
            {
                new Category { CategoryID = Guid.NewGuid(), Name = "Food & Dining", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Transportation", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Entertainment", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Housing", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Utilities", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Shopping", CreatedAt = now, UserId = testUser.Id }
            };

            var incomeCategories = new List<Category>
            {
                new Category { CategoryID = Guid.NewGuid(), Name = "Salary", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Freelance", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Gifts", CreatedAt = now, UserId = testUser.Id },
                new Category { CategoryID = Guid.NewGuid(), Name = "Investments", CreatedAt = now, UserId = testUser.Id }
            };

            await _dbContext.Categories.AddRangeAsync(expenseCategories);
            await _dbContext.Categories.AddRangeAsync(incomeCategories);
            await _dbContext.SaveChangesAsync();

            var allCategories = expenseCategories.Concat(incomeCategories).ToList();
            var random = new Random();

            // Create wallets and transactions in a single pass
            var walletNames = new[] { "Cash Wallet", "Bank Account", "Credit Card" };

            foreach (var walletName in walletNames)
            {
                // Create the wallet with zero initial balance
                var wallet = new Wallet
                {
                    WalletID = Guid.NewGuid(),
                    WalletName = walletName,
                    Balance = 0, // Start with zero, will update after creating transactions
                    UserId = testUser.Id,
                    User= testUser,
                };

                _dbContext.Wallets.Add(wallet);
                await _dbContext.SaveChangesAsync();

                // Generate 5-10 transactions for this wallet
                var transactions = new List<Transaction>();
                int transactionCount = random.Next(5, 11);

                for (int i = 0; i < transactionCount; i++)
                {
                    var category = allCategories[random.Next(allCategories.Count)];
                    var isExpense = expenseCategories.Contains(category);

                    // Generate amounts: negative for expenses, positive for income
                    var amount = isExpense
                        ? random.Next(50000, 500000) * -1M  // Expense (negative)
                        : random.Next(100000, 1000000) * 1M; // Income (positive)

                    var daysAgo = random.Next(0, 30);
                    var date = DateTime.Now.AddDays(-daysAgo);

                    var transaction = new Transaction
                    {
                        TransactionID = Guid.NewGuid(),
                        Amount = amount,
                        Description = $"{category.Name} transaction",
                        TransactionDate = date,
                        WalletID = wallet.WalletID,
                        Wallet = wallet,
                        CategoryID = category.CategoryID,
                        Category = category,
                        Type = isExpense ? "expense" : "income"// Correctly set the type
                    };

                    transactions.Add(transaction);
                    wallet.Balance += amount; // Update the wallet balance based on transaction amount
                }

                await _dbContext.Transactions.AddRangeAsync(transactions);
                _dbContext.Wallets.Update(wallet); // Update the wallet with the new balance
                await _dbContext.SaveChangesAsync();
            }
        }
    }

}

