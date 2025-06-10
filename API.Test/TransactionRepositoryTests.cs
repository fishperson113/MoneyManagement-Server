using API.Data;
using API.Helpers;
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Moq;

namespace API.Test
{
    [TestFixture]
    public class TransactionRepositoryTests
    {
        private ApplicationDbContext context;
        private IMapper mapper;
        private ILogger<TransactionRepository> logger;
        private ITransactionRepository transactionRepository;
        private IHttpContextAccessor httpContextAccessor;
        private readonly string currentUserId = "test-user-id";
        private readonly string otherUserId = "other-user-id";
        private ApplicationUser currentUser;
        private ApplicationUser otherUser;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new ApplicationDbContext(options);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ApplicationMapper>();
            });
            mapper = config.CreateMapper();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            logger = loggerFactory.CreateLogger<TransactionRepository>();

            // Mock HttpContextAccessor to return a fake user ID
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId)
            };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.User).Returns(claimsPrincipal);

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(m => m.HttpContext).Returns(httpContextMock.Object);

            httpContextAccessor = httpContextAccessorMock.Object;

            transactionRepository = new TransactionRepository(context, mapper, logger, httpContextAccessor);

            // Create test users
            currentUser = new ApplicationUser { Id = currentUserId, UserName = "CurrentUser" };
            otherUser = new ApplicationUser { Id = otherUserId, UserName = "OtherUser" };

            // Add users to context
            context.Users.Add(currentUser);
            context.Users.Add(otherUser);
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            context?.Dispose();
        }

        // Test to ensure we only get transactions from wallets belonging to the current user
        [Test]
        public async Task GetAllTransactionsAsync_ShouldOnlyReturnCurrentUserTransactions()
        {
            // Arrange
            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                User =currentUser,
                Balance = 1000
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User= otherUser
            };

            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Current User Transaction",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 200,
                    Description = "Other User Transaction",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.Add(category);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetAllTransactionsAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(1), "Should return only 1 transaction (current user's)");
                Assert.That(result.First().WalletID, Is.EqualTo(currentUserWallet.WalletID), "Transaction should be from current user's wallet");
                Assert.That(result.First().Description, Is.EqualTo("Current User Transaction"), "Description should match");
            });
        }

        // Test to verify GetTransactionByIdAsync only returns transaction if it belongs to current user
        [Test]
        public async Task GetTransactionByIdAsync_ShouldReturnNullForOtherUserTransaction()
        {
            // Arrange
            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                Balance = 1000,
                User= currentUser
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User = otherUser
            };

            var otherUserTransactionId = Guid.NewGuid();
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Current User Transaction",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = otherUserTransactionId,
                    CategoryID = category.CategoryID,
                    Amount = 200,
                    Description = "Other User Transaction",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.Add(category);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetTransactionByIdAsync(otherUserTransactionId);

            // Assert
            Assert.That(result, Is.Null, "Should return null for another user's transaction");
        }

        // Test to verify DeleteTransactionByIdAsync throws for other user's transaction
        [Test]
        public async Task DeleteTransactionByIdAsync_ShouldThrowForOtherUserTransaction()
        {
            // Arrange
            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                Balance = 1000,
                User= currentUser
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User= otherUser
            };

            var otherUserTransactionId = Guid.NewGuid();
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Current User Transaction",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = otherUserTransactionId,
                    CategoryID = category.CategoryID,
                    Amount = 200,
                    Description = "Other User Transaction",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.Add(category);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await transactionRepository.DeleteTransactionByIdAsync(otherUserTransactionId),
                "Should throw KeyNotFoundException for another user's transaction");
        }

        // Test to verify GetTransactionsByWalletIdAsync throws for other user's wallet
        [Test]
        public async Task GetTransactionsByWalletIdAsync_ShouldThrowForOtherUserWallet()
        {
            // Arrange
            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User= otherUser
            };

            context.Wallets.Add(otherUserWallet);
            await context.SaveChangesAsync();

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await transactionRepository.GetTransactionsByWalletIdAsync(otherUserWallet.WalletID),
                "Should throw UnauthorizedAccessException for another user's wallet");
        }

        // Test to verify CreateTransactionAsync throws for other user's wallet
        [Test]
        public async Task CreateTransactionAsync_ShouldThrowForOtherUserWallet()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var category = new Category
            {
                CategoryID = categoryId,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User= otherUser
            };

            context.Categories.Add(category);
            context.Wallets.Add(otherUserWallet);
            await context.SaveChangesAsync();

            var createTransactionDTO = new CreateTransactionDTO
            {
                CategoryID = categoryId,
                Amount = 100,
                Description = "Test Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = otherUserWallet.WalletID,
                Type= "income"
            };

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await transactionRepository.CreateTransactionAsync(createTransactionDTO),
                "Should throw UnauthorizedAccessException for creating transaction in another user's wallet");
        }

        // Test to verify UpdateTransactionAsync throws for other user's transaction
        [Test]
        public async Task UpdateTransactionAsync_ShouldThrowForOtherUserTransaction()
        {
            // Arrange
            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User=otherUser
            };

            var otherUserTransactionId = Guid.NewGuid();
            var transaction = new Transaction
            {
                TransactionID = otherUserTransactionId,
                CategoryID = category.CategoryID,
                Amount = 200,
                Description = "Other User Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = otherUserWallet.WalletID,
                Type = "income",
                Category = category,
                Wallet = otherUserWallet
            };

            context.Categories.Add(category);
            context.Wallets.Add(otherUserWallet);
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            var updateTransactionDTO = new UpdateTransactionDTO
            {
                TransactionID = otherUserTransactionId,
                CategoryID = category.CategoryID,
                Amount = 300,
                Description = "Updated Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = otherUserWallet.WalletID,
                Type = "income"
            };

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await transactionRepository.UpdateTransactionAsync(updateTransactionDTO),
                "Should throw KeyNotFoundException for updating another user's transaction");
        }

        // Test to verify GetCashFlowSummaryAsync only includes current user's transactions
        [Test]
        public async Task GetCashFlowSummaryAsync_ShouldOnlyIncludeCurrentUserTransactions()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;

            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                Balance = 1000,
                User=currentUser
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User=otherUser
            };

            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Current User Income",
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = -50,
                    Description = "Current User Expense",
                    TransactionDate = DateTime.UtcNow.AddDays(-2),
                    WalletID = currentUserWallet.WalletID,
                    Type = "expense",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 500,
                    Description = "Other User Income",
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.Add(category);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetCashFlowSummaryAsync(startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TotalIncome, Is.EqualTo(100), "TotalIncome should only include current user's income");
                Assert.That(result.TotalExpenses, Is.EqualTo(50), "TotalExpenses should only include current user's expenses");
                Assert.That(result.NetCashFlow, Is.EqualTo(50), "NetCashFlow should only reflect current user's transactions");
            });
        }

       
        // Test to verify GetDailySummaryAsync only includes current user's transactions
        [Test]
        public async Task GetDailySummaryAsync_ShouldOnlyIncludeCurrentUserTransactions()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;

            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                Balance = 1000,
                User= currentUser
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User= otherUser
            };

            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Current User Income",
                    TransactionDate = today.AddHours(10),
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = -50,
                    Description = "Current User Expense",
                    TransactionDate = today.AddHours(15),
                    WalletID = currentUserWallet.WalletID,
                    Type = "expense",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 500,
                    Description = "Other User Income",
                    TransactionDate = today.AddHours(12),
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.Add(category);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetDailySummaryAsync(today);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TotalIncome, Is.EqualTo(100), "TotalIncome should only include current user's income");
                Assert.That(result.TotalExpenses, Is.EqualTo(50), "TotalExpenses should only include current user's expenses");
                Assert.That(result.Transactions.Count, Is.EqualTo(2), "Should include only 2 transactions (current user's)");

                // Verify no other user transactions are included
                Assert.That(result.Transactions.Any(t => t.Description == "Other User Income"), Is.False,
                    "Should not include other user's transactions");
            });
        }

        [Test]
        public async Task GetTransactionsByDateRangeAsync_ShouldOnlyIncludeCurrentUserTransactions()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;

            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                Balance = 1000,
                User= currentUser
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User= otherUser
            };

            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Current User Transaction 1",
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = -50,
                    Description = "Current User Transaction 2",
                    TransactionDate = DateTime.UtcNow.AddDays(-2),
                    WalletID = currentUserWallet.WalletID,
                    Type = "expense",
                    Category = category,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 200,
                    Description = "Other User Transaction",
                    TransactionDate = DateTime.UtcNow.AddDays(-4),
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.Add(category);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Should return only current user's 2 transactions");

                // Verify no other user transactions are included
                Assert.That(result.Any(t => t.Description == "Other User Transaction"), Is.False,
                    "Should not include other user's transactions");
            });
        }

        [Test]
        public async Task GetMonthlySummaryAsync_ShouldOnlyIncludeCurrentUserTransactions()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var monthDate = new DateTime(today.Year, today.Month, 1); // First day of current month

            var foodCategory = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Food",
                CreatedAt = DateTime.UtcNow
            };

            var salaryCategory = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Salary",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                Balance = 1000,
                User=currentUser
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 2000,
                User= otherUser
            };

            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = salaryCategory.CategoryID,
                    Amount = 1000,
                    Description = "Monthly Salary",
                    TransactionDate = monthDate.AddDays(5),
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = salaryCategory,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = foodCategory.CategoryID,
                    Amount = -200,
                    Description = "Grocery Shopping",
                    TransactionDate = monthDate.AddDays(10),
                    WalletID = currentUserWallet.WalletID,
                    Type = "expense",
                    Category = foodCategory,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = foodCategory.CategoryID,
                    Amount = -100,
                    Description = "Restaurant Dinner",
                    TransactionDate = monthDate.AddDays(15),
                    WalletID = currentUserWallet.WalletID,
                    Type = "expense",
                    Category = foodCategory,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = salaryCategory.CategoryID,
                    Amount = 2000,
                    Description = "Other User Salary",
                    TransactionDate = monthDate.AddDays(5),
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = salaryCategory,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.AddRange(foodCategory, salaryCategory);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetMonthlySummaryAsync(monthDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Month, Is.EqualTo(monthDate.Month), "Month should match");
                Assert.That(result.Year, Is.EqualTo(monthDate.Year), "Year should match");
                Assert.That(result.MonthName, Is.EqualTo(monthDate.ToString("MMMM")), "Month name should match");

                Assert.That(result.TotalIncome, Is.EqualTo(1000), "Total income should only include current user's income");
                Assert.That(result.TotalExpenses, Is.EqualTo(300), "Total expenses should be 200 + 100 = 300");
                Assert.That(result.NetCashFlow, Is.EqualTo(700), "Net cash flow should be 1000 - 300 = 700");

                Assert.That(result.Transactions.Count, Is.EqualTo(3), "Should include 3 transactions from current user");
                Assert.That(result.Transactions.Any(t => t.Description == "Other User Salary"), Is.False,
                    "Should not include other user's transactions");

                // Check daily totals
                Assert.That(result.DailyTotals.Count, Is.EqualTo(3), "Should have totals for 3 days");
                Assert.That(result.DailyTotals.ContainsKey(6), Is.True, "Should contain day 6");
                Assert.That(result.DailyTotals[6], Is.EqualTo(1000), "Day 6 total should be 1000");
                Assert.That(result.DailyTotals.ContainsKey(11), Is.True, "Should contain day 11");
                Assert.That(result.DailyTotals[11], Is.EqualTo(-200), "Day 11 total should be -200");
                Assert.That(result.DailyTotals.ContainsKey(16), Is.True, "Should contain day 16");
                Assert.That(result.DailyTotals[16], Is.EqualTo(-100), "Day 16 total should be -100");

                // Check category totals
                Assert.That(result.CategoryTotals.Count, Is.EqualTo(2), "Should have totals for 2 categories");
                Assert.That(result.CategoryTotals.ContainsKey("Food"), Is.True, "Should contain Food category");
                Assert.That(result.CategoryTotals["Food"], Is.EqualTo(-300), "Food total should be -300");
                Assert.That(result.CategoryTotals.ContainsKey("Salary"), Is.True, "Should contain Salary category");
                Assert.That(result.CategoryTotals["Salary"], Is.EqualTo(1000), "Salary total should be 1000");
            });
        }

        [Test]
        public async Task GetYearlySummaryAsync_ShouldOnlyIncludeCurrentUserTransactions()
        {
            // Arrange
            int year = DateTime.UtcNow.Year;

            var incomeCategory = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Income",
                CreatedAt = DateTime.UtcNow
            };

            var housingCategory = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Housing",
                CreatedAt = DateTime.UtcNow
            };

            var foodCategory = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Food",
                CreatedAt = DateTime.UtcNow
            };

            var currentUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Current User Wallet",
                UserId = currentUserId,
                Balance = 10000,
                User= currentUser
            };

            var otherUserWallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Other User Wallet",
                UserId = otherUserId,
                Balance = 5000,
                User= otherUser
            };

            var transactions = new List<Transaction>
            {
                // Q1 transactions
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = incomeCategory.CategoryID,
                    Amount = 3000,
                    Description = "Q1 Salary",
                    TransactionDate = new DateTime(year, 1, 15),
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = incomeCategory,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = housingCategory.CategoryID,
                    Amount = -1000,
                    Description = "Q1 Rent",
                    TransactionDate = new DateTime(year, 1, 5),
                    WalletID = currentUserWallet.WalletID,
                    Type = "expense",
                    Category = housingCategory,
                    Wallet = currentUserWallet
                },
        
                // Q2 transactions
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = incomeCategory.CategoryID,
                    Amount = 3000,
                    Description = "Q2 Salary",
                    TransactionDate = new DateTime(year, 4, 15),
                    WalletID = currentUserWallet.WalletID,
                    Type = "income",
                    Category = incomeCategory,
                    Wallet = currentUserWallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = foodCategory.CategoryID,
                    Amount = -500,
                    Description = "Q2 Groceries",
                    TransactionDate = new DateTime(year, 4, 20),
                    WalletID = currentUserWallet.WalletID,
                    Type = "expense",
                    Category = foodCategory,
                    Wallet = currentUserWallet
                },
        
                // Other user's transaction
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = incomeCategory.CategoryID,
                    Amount = 5000,
                    Description = "Other User Income",
                    TransactionDate = new DateTime(year, 1, 10),
                    WalletID = otherUserWallet.WalletID,
                    Type = "income",
                    Category = incomeCategory,
                    Wallet = otherUserWallet
                }
            };

            context.Categories.AddRange(incomeCategory, housingCategory, foodCategory);
            context.Wallets.AddRange(currentUserWallet, otherUserWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetYearlySummaryAsync(year);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Year, Is.EqualTo(year), "Year should match");

                Assert.That(result.TotalIncome, Is.EqualTo(6000), "Total income should be 3000 + 3000 = 6000");
                Assert.That(result.TotalExpenses, Is.EqualTo(1500), "Total expenses should be 1000 + 500 = 1500");
                Assert.That(result.NetCashFlow, Is.EqualTo(4500), "Net cash flow should be 6000 - 1500 = 4500");

                Assert.That(result.Transactions.Count, Is.EqualTo(4), "Should include 4 transactions from current user");
                Assert.That(result.Transactions.Any(t => t.Description == "Other User Income"), Is.False,
                    "Should not include other user's transactions");

                // Check monthly totals
                Assert.That(result.MonthlyTotals.Count, Is.EqualTo(2), "Should have totals for 2 months");
                Assert.That(result.MonthlyTotals.ContainsKey("January"), Is.True, "Should contain January");
                Assert.That(result.MonthlyTotals["January"], Is.EqualTo(2000), "January total should be 3000 - 1000 = 2000");
                Assert.That(result.MonthlyTotals.ContainsKey("April"), Is.True, "Should contain April");
                Assert.That(result.MonthlyTotals["April"], Is.EqualTo(2500), "April total should be 3000 - 500 = 2500");

                // Check category totals
                Assert.That(result.CategoryTotals.Count, Is.EqualTo(3), "Should have totals for 3 categories");
                Assert.That(result.CategoryTotals.ContainsKey("Income"), Is.True, "Should contain Income category");
                Assert.That(result.CategoryTotals["Income"], Is.EqualTo(6000), "Income total should be 3000 + 3000 = 6000");
                Assert.That(result.CategoryTotals.ContainsKey("Housing"), Is.True, "Should contain Housing category");
                Assert.That(result.CategoryTotals["Housing"], Is.EqualTo(-1000), "Housing total should be -1000");
                Assert.That(result.CategoryTotals.ContainsKey("Food"), Is.True, "Should contain Food category");
                Assert.That(result.CategoryTotals["Food"], Is.EqualTo(-500), "Food total should be -500");

                // Check quarterly totals
                Assert.That(result.QuarterlyTotals.Count, Is.EqualTo(4), "Should have totals for 4 quarters");
                Assert.That(result.QuarterlyTotals["Q1"], Is.EqualTo(2000), "Q1 total should be 3000 - 1000 = 2000");
                Assert.That(result.QuarterlyTotals["Q2"], Is.EqualTo(2500), "Q2 total should be 3000 - 500 = 2500");
                Assert.That(result.QuarterlyTotals["Q3"], Is.EqualTo(0), "Q3 total should be 0");
                Assert.That(result.QuarterlyTotals["Q4"], Is.EqualTo(0), "Q4 total should be 0");
            });
        }

        [Test]
        public async Task GetTransactionsByDateRangeAsync_WithNoMatchingTransactions_ShouldReturnEmptyList()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddMonths(1);
            var startDate = futureDate;
            var endDate = futureDate.AddDays(7);

            // Act
            var result = await transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(0), "Should return an empty list");
            });
        }

        [Test]
        public async Task GetWeeklySummaryAsync_WithNoTransactions_ShouldReturnEmptySummary()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddMonths(1);
            var weekStartDate = new DateTime(futureDate.Year, futureDate.Month, 1); // Future month's first day

            // Act
            var result = await transactionRepository.GetWeeklySummaryAsync(weekStartDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.StartDate, Is.EqualTo(weekStartDate), "Start date should match");
                Assert.That(result.TotalIncome, Is.EqualTo(0), "Total income should be 0");
                Assert.That(result.TotalExpenses, Is.EqualTo(0), "Total expenses should be 0");
                Assert.That(result.NetCashFlow, Is.EqualTo(0), "Net cash flow should be 0");
                Assert.That(result.Transactions, Is.Empty, "Transactions list should be empty");
                Assert.That(result.DailyTotals, Is.Empty, "Daily totals should be empty");
                Assert.That(result.DailyExpenseTotals, Is.Empty, "Daily expense totals should be empty");
                Assert.That(result.DailyIncomeTotals, Is.Empty, "Daily income totals should be empty");
            });
        }

        [Test]
        public async Task GetMonthlySummaryAsync_WithNoTransactions_ShouldReturnEmptySummary()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddMonths(1);
            var monthDate = new DateTime(futureDate.Year, futureDate.Month, 1); // Future month's first day

            // Act
            var result = await transactionRepository.GetMonthlySummaryAsync(monthDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Month, Is.EqualTo(monthDate.Month), "Month should match");
                Assert.That(result.Year, Is.EqualTo(monthDate.Year), "Year should match");
                Assert.That(result.TotalIncome, Is.EqualTo(0), "Total income should be 0");
                Assert.That(result.TotalExpenses, Is.EqualTo(0), "Total expenses should be 0");
                Assert.That(result.NetCashFlow, Is.EqualTo(0), "Net cash flow should be 0");
                Assert.That(result.Transactions, Is.Empty, "Transactions list should be empty");
                Assert.That(result.DailyTotals, Is.Empty, "Daily totals should be empty");
                Assert.That(result.CategoryTotals, Is.Empty, "Category totals should be empty");
            });
        }

        [Test]
        public async Task GetYearlySummaryAsync_WithNoTransactions_ShouldReturnEmptySummary()
        {
            // Arrange
            int futureYear = DateTime.UtcNow.Year + 1;

            // Act
            var result = await transactionRepository.GetYearlySummaryAsync(futureYear);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Year, Is.EqualTo(futureYear), "Year should match");
                Assert.That(result.TotalIncome, Is.EqualTo(0), "Total income should be 0");
                Assert.That(result.TotalExpenses, Is.EqualTo(0), "Total expenses should be 0");
                Assert.That(result.NetCashFlow, Is.EqualTo(0), "Net cash flow should be 0");
                Assert.That(result.Transactions, Is.Empty, "Transactions list should be empty");
                Assert.That(result.MonthlyTotals, Is.Empty, "Monthly totals should be empty");
                Assert.That(result.CategoryTotals, Is.Empty, "Category totals should be empty");

                // Quarterly totals should have 0 values for all quarters
                Assert.That(result.QuarterlyTotals.Count, Is.EqualTo(4), "Should have totals for 4 quarters");
                Assert.That(result.QuarterlyTotals["Q1"], Is.EqualTo(0), "Q1 total should be 0");
                Assert.That(result.QuarterlyTotals["Q2"], Is.EqualTo(0), "Q2 total should be 0");
                Assert.That(result.QuarterlyTotals["Q3"], Is.EqualTo(0), "Q3 total should be 0");
                Assert.That(result.QuarterlyTotals["Q4"], Is.EqualTo(0), "Q4 total should be 0");
            });
        }
    }
}
