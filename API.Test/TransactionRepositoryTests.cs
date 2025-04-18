using API.Data;
using API.Helpers;
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Test
{
    [TestFixture]
    public class TransactionRepositoryTests
    {
        private ApplicationDbContext context;
        private IMapper mapper;
        private ILogger<TransactionRepository> logger;
        private ITransactionRepository transactionRepository;

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

            transactionRepository = new TransactionRepository(context, mapper, logger);
        }

        [TearDown]
        public void TearDown()
        {
            context?.Dispose();
        }

        [Test]
        public async Task CreateTransactionAsync_ShouldCreateTransaction()
        {
            // Arrange
            var createTransactionDTO = new CreateTransactionDTO
            {
                CategoryID = Guid.NewGuid(),
                Amount = 100,
                Description = "Test Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = Guid.NewGuid()
            };
            var category = new Category
            {
                CategoryID = createTransactionDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };
            var wallet = new Wallet
            {
                WalletID = createTransactionDTO.WalletID,
                WalletName = "Test Wallet",
                Balance = 1000,
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.CreateTransactionAsync(createTransactionDTO);

            // Assert
            var createdTransaction = await context.Transactions.FindAsync(result.TransactionID);
            Assert.Multiple(() =>
            {
                Assert.That(createdTransaction, Is.Not.Null, "Transaction should exist in the database");
                Assert.That(createdTransaction?.CategoryID, Is.EqualTo(createTransactionDTO.CategoryID), "CategoryID should match");
                Assert.That(createdTransaction?.Amount, Is.EqualTo(createTransactionDTO.Amount), "Amount should match");
                Assert.That(createdTransaction?.Description, Is.EqualTo(createTransactionDTO.Description), "Description should match");
                Assert.That(createdTransaction?.TransactionDate, Is.EqualTo(createTransactionDTO.TransactionDate), "TransactionDate should match");
                Assert.That(createdTransaction?.WalletID, Is.EqualTo(createTransactionDTO.WalletID), "WalletID should match");
                Assert.That(createdTransaction?.Type, Is.EqualTo("income"), "Type should be automatically set to 'income' based on positive amount");
            });
        }

        [Test]
        public async Task UpdateTransactionAsync_ShouldUpdateTransaction()
        {
            // Arrange
            var updateTransactionDTO = new UpdateTransactionDTO
            {
                TransactionID = Guid.NewGuid(),
                CategoryID = Guid.NewGuid(),
                Amount = 200,
                Description = "Updated Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = Guid.NewGuid()
            };
            var category = new Category
            {
                CategoryID = updateTransactionDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };
            var wallet = new Wallet
            {
                WalletID = updateTransactionDTO.WalletID,
                WalletName = "Test Wallet",
                Balance = 1000,
            };
            var transaction = new Transaction
            {
                TransactionID = updateTransactionDTO.TransactionID,
                CategoryID = updateTransactionDTO.CategoryID,
                Amount = 100,
                Description = "Old Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = updateTransactionDTO.WalletID,
                Type = "income", // Set initial type
                Category = category,
                Wallet = wallet
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.UpdateTransactionAsync(updateTransactionDTO);

            // Assert
            var updatedTransaction = await context.Transactions.FindAsync(result?.TransactionID);
            Assert.Multiple(() =>
            {
                Assert.That(updatedTransaction, Is.Not.Null, "Transaction should exist in the database");
                Assert.That(updatedTransaction?.CategoryID, Is.EqualTo(updateTransactionDTO.CategoryID), "CategoryID should match");
                Assert.That(updatedTransaction?.Amount, Is.EqualTo(updateTransactionDTO.Amount), "Amount should match");
                Assert.That(updatedTransaction?.Description, Is.EqualTo(updateTransactionDTO.Description), "Description should match");
                Assert.That(updatedTransaction?.TransactionDate, Is.EqualTo(updateTransactionDTO.TransactionDate), "TransactionDate should match");
                Assert.That(updatedTransaction?.WalletID, Is.EqualTo(updateTransactionDTO.WalletID), "WalletID should match");
                Assert.That(updatedTransaction?.Type, Is.EqualTo("income"), "Type should be automatically updated based on positive amount");
            });
        }

        [Test]
        public async Task DeleteTransactionByIdAsync_ShouldDeleteTransaction()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };
            var wallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Test Wallet",
                Balance = 1000
            };
            var transaction = new Transaction
            {
                TransactionID = transactionId,
                CategoryID = category.CategoryID,
                Amount = 100,
                Description = "Test Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = wallet.WalletID,
                Type = "income", // Set type based on amount
                Category = category,
                Wallet = wallet
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.DeleteTransactionByIdAsync(transactionId);

            // Assert
            var deletedTransaction = await context.Transactions.FindAsync(result);
            Assert.That(deletedTransaction, Is.Null, "Deleted TransactionID should not be found");
        }
        [Test]
        public async Task UpdateTransactionAsync_WithExplicitType_ShouldRespectProvidedType()
        {
            // Arrange
            var updateTransactionDTO = new UpdateTransactionDTO
            {
                TransactionID = Guid.NewGuid(),
                CategoryID = Guid.NewGuid(),
                Amount = 200, // Positive amount would normally be "income"
                Description = "Updated Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = Guid.NewGuid(),
                Type = "expense" // But we explicitly set it as expense
            };
            var category = new Category
            {
                CategoryID = updateTransactionDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };
            var wallet = new Wallet
            {
                WalletID = updateTransactionDTO.WalletID,
                WalletName = "Test Wallet",
                Balance = 1000,
            };
            var transaction = new Transaction
            {
                TransactionID = updateTransactionDTO.TransactionID,
                CategoryID = updateTransactionDTO.CategoryID,
                Amount = 100,
                Description = "Old Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = updateTransactionDTO.WalletID,
                Type = "income", // Initial type
                Category = category,
                Wallet = wallet
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.UpdateTransactionAsync(updateTransactionDTO);

            // Assert
            var updatedTransaction = await context.Transactions.FindAsync(result?.TransactionID);
            Assert.Multiple(() =>
            {
                Assert.That(updatedTransaction, Is.Not.Null, "Transaction should exist in the database");
                Assert.That(updatedTransaction?.Type, Is.EqualTo("expense"), "Type should be 'expense' as explicitly set");
            });
        }

        [Test]
        public async Task GetAllTransactionsAsync_ShouldReturnAllTransactions()
        {
            // Arrange
            var category1 = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Category 1",
                CreatedAt = DateTime.UtcNow
            };
            var category2 = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Category 2",
                CreatedAt = DateTime.UtcNow
            };
            var wallet1 = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Wallet 1",
                Balance = 1000
            };
            var wallet2 = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Wallet 2",
                Balance = 2000
            };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category1.CategoryID,
                    Amount = 100,
                    Description = "Transaction 1",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = wallet1.WalletID,
                    Type = "income",
                    Category = category1,
                    Wallet = wallet1
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category2.CategoryID,
                    Amount = 200,
                    Description = "Transaction 2",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = wallet2.WalletID,
                    Type = "income",
                    Category = category2,
                    Wallet = wallet2
                }
            };

            context.Categories.AddRange(category1, category2);
            context.Wallets.AddRange(wallet1, wallet2);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetAllTransactionsAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Result count should be 2");

                var resultList = result.ToList();
                Assert.That(resultList[0].CategoryID, Is.EqualTo(transactions[0].CategoryID), "First transaction CategoryID should match");
                Assert.That(resultList[0].Amount, Is.EqualTo(transactions[0].Amount), "First transaction Amount should match");
                Assert.That(resultList[0].Description, Is.EqualTo(transactions[0].Description), "First transaction Description should match");
                Assert.That(resultList[0].TransactionDate, Is.EqualTo(transactions[0].TransactionDate), "First transaction TransactionDate should match");
                Assert.That(resultList[0].WalletID, Is.EqualTo(transactions[0].WalletID), "First transaction WalletID should match");
                Assert.That(resultList[0].Type, Is.EqualTo(transactions[0].Type), "First transaction Type should match");

                Assert.That(resultList[1].CategoryID, Is.EqualTo(transactions[1].CategoryID), "Second transaction CategoryID should match");
                Assert.That(resultList[1].Amount, Is.EqualTo(transactions[1].Amount), "Second transaction Amount should match");
                Assert.That(resultList[1].Description, Is.EqualTo(transactions[1].Description), "Second transaction Description should match");
                Assert.That(resultList[1].TransactionDate, Is.EqualTo(transactions[1].TransactionDate), "Second transaction TransactionDate should match");
                Assert.That(resultList[1].WalletID, Is.EqualTo(transactions[1].WalletID), "Second transaction WalletID should match");
                Assert.That(resultList[1].Type, Is.EqualTo(transactions[1].Type), "Second transaction Type should match");
            });
        }

        [Test]
        public async Task GetTransactionByIdAsync_ShouldReturnTransaction()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };
            var wallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Test Wallet",
                Balance = 1000
            };
            var transaction = new Transaction
            {
                TransactionID = transactionId,
                CategoryID = category.CategoryID,
                Amount = 100,
                Description = "Test Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = wallet.WalletID,
                Type = "income",
                Category = category,
                Wallet = wallet
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetTransactionByIdAsync(transactionId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Transaction should exist in the database");
                Assert.That(result?.CategoryID, Is.EqualTo(transaction.CategoryID), "CategoryID should match");
                Assert.That(result?.Amount, Is.EqualTo(transaction.Amount), "Amount should match");
                Assert.That(result?.Description, Is.EqualTo(transaction.Description), "Description should match");
                Assert.That(result?.TransactionDate, Is.EqualTo(transaction.TransactionDate), "TransactionDate should match");
                Assert.That(result?.WalletID, Is.EqualTo(transaction.WalletID), "WalletID should match");
                Assert.That(result?.Type, Is.EqualTo(transaction.Type), "Type should match");
            });
        }

        [Test]
        public async Task GetTransactionsByWalletIdAsync_ShouldReturnTransactionsForWallet()
        {
            // Arrange
            var walletId = Guid.NewGuid();
            var otherWalletId = Guid.NewGuid();
            var category = new Category
            {
                CategoryID = Guid.NewGuid(),
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };
            var wallet = new Wallet
            {
                WalletID = walletId,
                WalletName = "Test Wallet",
                Balance = 1000
            };
            var otherWallet = new Wallet
            {
                WalletID = otherWalletId,
                WalletName = "Other Wallet",
                Balance = 2000
            };

            var transactions = new List<Transaction>
    {
        new Transaction {
            TransactionID = Guid.NewGuid(),
            CategoryID = category.CategoryID,
            Amount = 100,
            Description = "Wallet Transaction 1",
            TransactionDate = DateTime.UtcNow,
            WalletID = walletId,
            Type = "income",
            Category = category,
            Wallet = wallet
        },
        new Transaction {
            TransactionID = Guid.NewGuid(),
            CategoryID = category.CategoryID,
            Amount = 200,
            Description = "Wallet Transaction 2",
            TransactionDate = DateTime.UtcNow,
            WalletID = walletId,
            Type = "income",
            Category = category,
            Wallet = wallet
        },
        new Transaction {
            TransactionID = Guid.NewGuid(),
            CategoryID = category.CategoryID,
            Amount = -300,
            Description = "Other Wallet Transaction",
            TransactionDate = DateTime.UtcNow,
            WalletID = otherWalletId,
            Type = "expense",
            Category = category,
            Wallet = otherWallet
         }
    };

            context.Categories.Add(category);
            context.Wallets.AddRange(wallet, otherWallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetTransactionsByWalletIdAsync(walletId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Should return exactly 2 transactions");

                var resultList = result.ToList();
                Assert.That(resultList[0].CategoryID, Is.EqualTo(transactions[0].CategoryID), "First transaction CategoryID should match");
                Assert.That(resultList[0].Amount, Is.EqualTo(transactions[0].Amount), "First transaction Amount should match");
                Assert.That(resultList[0].Description, Is.EqualTo(transactions[0].Description), "First transaction Description should match");
                Assert.That(resultList[0].TransactionDate, Is.EqualTo(transactions[0].TransactionDate), "First transaction TransactionDate should match");
                Assert.That(resultList[0].WalletID, Is.EqualTo(transactions[0].WalletID), "First transaction WalletID should match");
                Assert.That(resultList[0].Type, Is.EqualTo(transactions[0].Type), "First transaction Type should match");

                Assert.That(resultList[1].CategoryID, Is.EqualTo(transactions[1].CategoryID), "Second transaction CategoryID should match");
                Assert.That(resultList[1].Amount, Is.EqualTo(transactions[1].Amount), "Second transaction Amount should match");
                Assert.That(resultList[1].Description, Is.EqualTo(transactions[1].Description), "Second transaction Description should match");
                Assert.That(resultList[1].TransactionDate, Is.EqualTo(transactions[1].TransactionDate), "Second transaction TransactionDate should match");
                Assert.That(resultList[1].WalletID, Is.EqualTo(transactions[1].WalletID), "Second transaction WalletID should match");
                Assert.That(resultList[1].Type, Is.EqualTo(transactions[1].Type), "Second transaction Type should match");
            });
        }
        [Test]
        public async Task GetTransactionsByDateRangeAsync_ShouldReturnFilteredTransactions()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Transaction 1",
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    WalletID = wallet.WalletID,
                    Type = "income",
                    Category = category,
                    Wallet = wallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = -50,
                    Description = "Transaction 2",
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    WalletID = wallet.WalletID,
                    Type = "expense",
                    Category = category,
                    Wallet = wallet
                }
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Should return exactly 2 transactions");
            });
        }
        [Test]
        public async Task GetTransactionsByDateRangeAsync_WithTypeFilter_ShouldReturnFilteredTransactions()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
    {
        new Transaction {
            TransactionID = Guid.NewGuid(),
            CategoryID = category.CategoryID,
            Amount = 100,
            Description = "Income Transaction",
            TransactionDate = DateTime.UtcNow.AddDays(-3),
            WalletID = wallet.WalletID,
            Type = "income",
            Category = category,
            Wallet = wallet
        },
        new Transaction {
            TransactionID = Guid.NewGuid(),
            CategoryID = category.CategoryID,
            Amount = -50,
            Description = "Expense Transaction",
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            WalletID = wallet.WalletID,
            Type = "expense",
            Category = category,
            Wallet = wallet
        }
    };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate, "income");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(1), "Should return exactly 1 transaction");
                Assert.That(result.First().Type, Is.EqualTo("income"), "Should return only income transactions");
            });
        }
        [Test]
        public async Task GetAggregateStatisticsAsync_ShouldReturnAggregatedData()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    Amount = 100,
                    TransactionDate = DateTime.UtcNow.AddDays(-10),
                    Type = "income",
                    Wallet = wallet,
                    Category = category
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    Amount = -50,
                    TransactionDate = DateTime.UtcNow.AddDays(-20),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category
                }
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetAggregateStatisticsAsync("daily", startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Any(), Is.True, "Should return aggregated statistics");

                // Verify the result contains both income and expense types
                var types = result.Select(r => r.Type).Distinct().ToList();
                Assert.That(types.Contains("income"), Is.True, "Should include income type");
                Assert.That(types.Contains("expense"), Is.True, "Should include expense type");
            });
        }

        [Test]
        public async Task GetAggregateStatisticsAsync_WithTypeFilter_ShouldReturnFilteredData()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    Amount = 100,
                    TransactionDate = DateTime.UtcNow.AddDays(-10),
                    Type = "income",
                    Wallet = wallet,
                    Category = category
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    Amount = -50,
                    TransactionDate = DateTime.UtcNow.AddDays(-20),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category
                }
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetAggregateStatisticsAsync("daily", startDate, endDate, "income");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Any(), Is.True, "Should return aggregated statistics");

                // Verify all results have income type
                foreach (var stat in result)
                {
                    Assert.That(stat.Type, Is.EqualTo("income"), "All results should have income type");
                }
            });
        }
        [Test]
        public async Task GetCategoryBreakdownAsync_ShouldReturnCategoryBreakdown()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    Type = "income",
                    Wallet = wallet,
                    Category = category
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = -50,
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category
                }
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetCategoryBreakdownAsync(startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Any(), Is.True, "Should return category breakdown");
                Assert.That(result.First().Category, Is.EqualTo(category.Name), "Category name should match");
                Assert.That(result.First().Total, Is.EqualTo(150), "Total should be the sum of absolute amounts (100 + 50)");
                Assert.That(result.First().Percentage, Is.EqualTo(100), "Percentage should be 100% since there's only one category");
            });
        }

        [Test]
        public async Task GetCategoryBreakdownAsync_WithTypeFilter_ShouldReturnFilteredData()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var category1 = new Category { CategoryID = Guid.NewGuid(), Name = "Food", CreatedAt = DateTime.UtcNow };
            var category2 = new Category { CategoryID = Guid.NewGuid(), Name = "Salary", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category1.CategoryID,
                    Amount = -50,
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category1
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category1.CategoryID,
                    Amount = -30,
                    TransactionDate = DateTime.UtcNow.AddDays(-4),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category1
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category2.CategoryID,
                    Amount = 1000,
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    Type = "income",
                    Wallet = wallet,
                    Category = category2
                }
            };

            context.Categories.AddRange(category1, category2);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetCategoryBreakdownAsync(startDate, endDate, "expense");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(1), "Should return only expense categories");
                Assert.That(result.First().Category, Is.EqualTo("Food"), "Category name should match");
                Assert.That(result.First().Total, Is.EqualTo(80), "Total should be the sum of food expenses (50 + 30)");
                Assert.That(result.First().Percentage, Is.EqualTo(100), "Percentage should be 100% since there's only one expense category");
            });
        }

        [Test]
        public async Task GetCashFlowSummaryAsync_ShouldReturnCashFlowSummary()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    Amount = 100,
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    Type = "income",
                    Wallet = wallet,
                    Category = category,
                    CategoryID = category.CategoryID,
                    WalletID = wallet.WalletID
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    Amount = -50,
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category,
                    CategoryID = category.CategoryID,
                    WalletID = wallet.WalletID
                }
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetCashFlowSummaryAsync(startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TotalIncome, Is.EqualTo(100), "Total income should match");
                Assert.That(result.TotalExpenses, Is.EqualTo(50), "Total expenses should match");
                Assert.That(result.NetCashFlow, Is.EqualTo(50), "Net cash flow should match");
            });
        }

        [Test]
        public async Task SearchTransactionsAsync_ShouldReturnFilteredTransactions()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Test Transaction",
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    Type = "income",
                    Wallet = wallet,
                    Category = category,
                    WalletID = wallet.WalletID
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = -50,
                    Description = "Another Transaction",
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category,
                    WalletID = wallet.WalletID
                }
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.SearchTransactionsAsync(startDate, endDate, null, "Test Category", null, "Test", null, null);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(1), "Should return exactly 1 transaction");
                Assert.That(result.First().Description, Is.EqualTo("Test Transaction"), "Description should match");
                Assert.That(result.First().Type, Is.EqualTo("income"), "Type should match");
            });
        }

        [Test]
        public async Task SearchTransactionsAsync_WithTypeFilter_ShouldReturnFilteredTransactions()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
            {
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 100,
                    Description = "Income Transaction",
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    Type = "income",
                    Wallet = wallet,
                    Category = category,
                    WalletID = wallet.WalletID
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = -50,
                    Description = "Expense Transaction",
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    Type = "expense",
                    Wallet = wallet,
                    Category = category,
                    WalletID = wallet.WalletID
                }
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.SearchTransactionsAsync(startDate, endDate, "expense", null, null, null, null, null);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(1), "Should return exactly 1 transaction");
                Assert.That(result.First().Description, Is.EqualTo("Expense Transaction"), "Description should match");
                Assert.That(result.First().Type, Is.EqualTo("expense"), "Type should be 'expense'");
            });
        }

        [Test]
        public async Task GetDailySummaryAsync_ShouldReturnDailySummary()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var wallet = new Wallet { WalletID = Guid.NewGuid(), WalletName = "Test Wallet", Balance = 1000 };
            var transactions = new List<Transaction>
    {
        new Transaction {
            TransactionID = Guid.NewGuid(),
            Amount = 100,
            TransactionDate = date.AddHours(10),
            Type = "income",
            Wallet = wallet,
            Category = category,
            CategoryID = category.CategoryID,
            WalletID = wallet.WalletID
        },
        new Transaction {
            TransactionID = Guid.NewGuid(),
            Amount = -50,
            TransactionDate = date.AddHours(15),
            Type = "expense",
            Wallet = wallet,
            Category = category,
            CategoryID = category.CategoryID,
            WalletID = wallet.WalletID
        }
    };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.GetDailySummaryAsync(date);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TotalIncome, Is.EqualTo(100), "Total income should match");
                Assert.That(result.TotalExpenses, Is.EqualTo(50), "Total expenses should match");
                Assert.That(result.Transactions.Count, Is.EqualTo(2), "Should include 2 transactions");

                // Check transaction types in the result
                var incomeTransaction = result.Transactions.FirstOrDefault(t => t.Type == "income");
                var expenseTransaction = result.Transactions.FirstOrDefault(t => t.Type == "expense");

                Assert.That(incomeTransaction, Is.Not.Null, "Should contain an income transaction");
                Assert.That(expenseTransaction, Is.Not.Null, "Should contain an expense transaction");
            });
        }

        [Test]
        public async Task GetDailySummaryAsync_WithNoTransactions_ShouldReturnEmptySummary()
        {
            // Arrange
            var date = DateTime.UtcNow.Date.AddDays(1); // Future date with no transactions

            // Act
            var result = await transactionRepository.GetDailySummaryAsync(date);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TotalIncome, Is.EqualTo(0), "Total income should be 0");
                Assert.That(result.TotalExpenses, Is.EqualTo(0), "Total expenses should be 0");
                Assert.That(result.Transactions, Is.Empty, "Transactions list should be empty");
            });
        }
        [Test]
        public async Task CreateTransactionAsync_WithExplicitType_ShouldRespectProvidedType()
        {
            // Arrange
            var createTransactionDTO = new CreateTransactionDTO
            {
                CategoryID = Guid.NewGuid(),
                Amount = 100, // Positive amount would normally be "income"
                Description = "Test Transaction",
                TransactionDate = DateTime.UtcNow,
                WalletID = Guid.NewGuid(),
                Type = "expense" // But we explicitly set it as expense
            };

            // Set up Category and Wallet
            var category = new Category
            {
                CategoryID = createTransactionDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow
            };
            var wallet = new Wallet
            {
                WalletID = createTransactionDTO.WalletID,
                WalletName = "Test Wallet",
                Balance = 1000,
            };

            context.Categories.Add(category);
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.CreateTransactionAsync(createTransactionDTO);

            // Assert
            var createdTransaction = await context.Transactions.FindAsync(result.TransactionID);
            Assert.That(createdTransaction, Is.Not.Null, "Transaction should exist in the database");
            Assert.That(createdTransaction?.Type, Is.EqualTo("expense"), "Type should be 'expense' as explicitly set");
        }
    }
}
