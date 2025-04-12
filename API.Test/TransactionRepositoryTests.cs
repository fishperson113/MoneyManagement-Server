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
                new Transaction { TransactionID = Guid.NewGuid(), CategoryID = category1.CategoryID, Amount = 100, Description = "Transaction 1", TransactionDate = DateTime.UtcNow, WalletID = wallet1.WalletID, Category = category1, Wallet = wallet1 },
                new Transaction { TransactionID = Guid.NewGuid(), CategoryID = category2.CategoryID, Amount = 200, Description = "Transaction 2", TransactionDate = DateTime.UtcNow, WalletID = wallet2.WalletID, Category = category2, Wallet = wallet2 }
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

                Assert.That(resultList[1].CategoryID, Is.EqualTo(transactions[1].CategoryID), "Second transaction CategoryID should match");
                Assert.That(resultList[1].Amount, Is.EqualTo(transactions[1].Amount), "Second transaction Amount should match");
                Assert.That(resultList[1].Description, Is.EqualTo(transactions[1].Description), "Second transaction Description should match");
                Assert.That(resultList[1].TransactionDate, Is.EqualTo(transactions[1].TransactionDate), "Second transaction TransactionDate should match");
                Assert.That(resultList[1].WalletID, Is.EqualTo(transactions[1].WalletID), "Second transaction WalletID should match");
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
                    Category = category,
                    Wallet = wallet
                },
                new Transaction {
                    TransactionID = Guid.NewGuid(),
                    CategoryID = category.CategoryID,
                    Amount = 300,
                    Description = "Other Wallet Transaction",
                    TransactionDate = DateTime.UtcNow,
                    WalletID = otherWalletId,
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

                Assert.That(resultList[1].CategoryID, Is.EqualTo(transactions[1].CategoryID), "Second transaction CategoryID should match");
                Assert.That(resultList[1].Amount, Is.EqualTo(transactions[1].Amount), "Second transaction Amount should match");
                Assert.That(resultList[1].Description, Is.EqualTo(transactions[1].Description), "Second transaction Description should match");
                Assert.That(resultList[1].TransactionDate, Is.EqualTo(transactions[1].TransactionDate), "Second transaction TransactionDate should match");
                Assert.That(resultList[1].WalletID, Is.EqualTo(transactions[1].WalletID), "Second transaction WalletID should match");
            });
        }
    }
}
