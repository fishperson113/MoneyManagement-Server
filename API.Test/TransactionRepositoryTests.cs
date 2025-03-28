using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
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
        private Mock<IMapper> mapperMock;
        private Mock<ILogger<TransactionRepository>> loggerMock;
        private ITransactionRepository transactionRepository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new ApplicationDbContext(options);

            mapperMock = new Mock<IMapper>();
            loggerMock = new Mock<ILogger<TransactionRepository>>();
            transactionRepository = new TransactionRepository(context, mapperMock.Object, loggerMock.Object);
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
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Test Wallet",
                Balance = 1000,
                User = new ApplicationUser()
            };
            var transaction = new Transaction
            {
                TransactionID = Guid.NewGuid(),
                CategoryID = createTransactionDTO.CategoryID,
                Amount = createTransactionDTO.Amount,
                Description = createTransactionDTO.Description,
                TransactionDate = createTransactionDTO.TransactionDate,
                WalletID = createTransactionDTO.WalletID,
                Category = category,
                Wallet = wallet
            };
            var transactionDTO = new TransactionDTO
            {
                TransactionID = transaction.TransactionID,
                CategoryID = transaction.CategoryID,
                Amount = transaction.Amount,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                WalletID = transaction.WalletID
            };

            mapperMock.Setup(m => m.Map<Transaction>(createTransactionDTO)).Returns(transaction);
            mapperMock.Setup(m => m.Map<TransactionDTO>(transaction)).Returns(transactionDTO);

            // Act
            var result = await transactionRepository.CreateTransactionAsync(createTransactionDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TransactionID, Is.EqualTo(transactionDTO.TransactionID), "TransactionID should match");
                Assert.That(result.Amount, Is.EqualTo(transactionDTO.Amount), "Amount should match");
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
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Test Wallet",
                Balance = 1000,
                User = new ApplicationUser()
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
            var transactionDTO = new TransactionDTO
            {
                TransactionID = updateTransactionDTO.TransactionID,
                CategoryID = updateTransactionDTO.CategoryID,
                Amount = updateTransactionDTO.Amount,
                Description = updateTransactionDTO.Description,
                TransactionDate = updateTransactionDTO.TransactionDate,
                WalletID = updateTransactionDTO.WalletID
            };

            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            mapperMock.Setup(m => m.Map<TransactionDTO>(transaction)).Returns(transactionDTO);

            // Act
            var result = await transactionRepository.UpdateTransactionAsync(updateTransactionDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TransactionID, Is.EqualTo(transactionDTO.TransactionID), "TransactionID should match");
                Assert.That(result.Amount, Is.EqualTo(transactionDTO.Amount), "Amount should match");
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
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Test Wallet",
                Balance = 1000,
                User = new ApplicationUser()
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

            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            // Act
            var result = await transactionRepository.DeleteTransactionByIdAsync(transactionId);

            // Assert
            Assert.That(result, Is.EqualTo(transactionId), "Deleted TransactionID should match");
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
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Wallet 1",
                Balance = 1000,
                User = new ApplicationUser()
            };
            var wallet2 = new Wallet
            {
                WalletID = Guid.NewGuid(),
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Wallet 2",
                Balance = 2000,
                User = new ApplicationUser()
            };
            var transactions = new List<Transaction>
            {
                new Transaction { TransactionID = Guid.NewGuid(), CategoryID = category1.CategoryID, Amount = 100, Description = "Transaction 1", TransactionDate = DateTime.UtcNow, WalletID = wallet1.WalletID, Category = category1, Wallet = wallet1 },
                new Transaction { TransactionID = Guid.NewGuid(), CategoryID = category2.CategoryID, Amount = 200, Description = "Transaction 2", TransactionDate = DateTime.UtcNow, WalletID = wallet2.WalletID, Category = category2, Wallet = wallet2 }
            };
            var transactionDTOs = new List<TransactionDTO>
            {
                new TransactionDTO { TransactionID = transactions[0].TransactionID, CategoryID = transactions[0].CategoryID, Amount = 100, Description = "Transaction 1", TransactionDate = DateTime.UtcNow, WalletID = transactions[0].WalletID },
                new TransactionDTO { TransactionID = transactions[1].TransactionID, CategoryID = transactions[1].CategoryID, Amount = 200, Description = "Transaction 2", TransactionDate = DateTime.UtcNow, WalletID = transactions[1].WalletID }
            };

            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            mapperMock.Setup(m => m.Map<IEnumerable<TransactionDTO>>(transactions)).Returns(transactionDTOs);

            // Act
            var result = await transactionRepository.GetAllTransactionsAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Result count should be 2");
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
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Test Wallet",
                Balance = 1000,
                User = new ApplicationUser()
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
            var transactionDTO = new TransactionDTO
            {
                TransactionID = transactionId,
                CategoryID = transaction.CategoryID,
                Amount = transaction.Amount,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                WalletID = transaction.WalletID
            };

            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            mapperMock.Setup(m => m.Map<TransactionDTO>(transaction)).Returns(transactionDTO);

            // Act
            var result = await transactionRepository.GetTransactionByIdAsync(transactionId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.TransactionID, Is.EqualTo(transactionDTO.TransactionID), "TransactionID should match");
                Assert.That(result.Amount, Is.EqualTo(transactionDTO.Amount), "Amount should match");
            });
        }
    }
}
