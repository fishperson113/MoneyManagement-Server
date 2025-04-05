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
    public class WalletRepositoryTests
    {
        private ApplicationDbContext context;
        private IMapper mapper;
        private ILogger<WalletRepository> logger;
        private IWalletRepository walletRepository;

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
            logger = loggerFactory.CreateLogger<WalletRepository>();

            walletRepository = new WalletRepository(context, mapper, logger);
        }

        [TearDown]
        public void TearDown()
        {
            context?.Dispose();
        }

        [Test]
        public async Task CreateWalletAsync_ShouldCreateWallet()
        {
            // Arrange
            var createWalletDTO = new CreateWalletDTO
            {
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Test Wallet",
                Balance = 1000
            };
            var user = new ApplicationUser
            {
                Id = createWalletDTO.UserID,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = Guid.NewGuid(),
                UserID = createWalletDTO.UserID,
                WalletName = createWalletDTO.WalletName,
                Balance = createWalletDTO.Balance,
                User = user
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.CreateWalletAsync(createWalletDTO);

            // Assert
            var createdWallet = await context.Wallets.FindAsync(result.WalletID);
            Assert.Multiple(() =>
            {
                Assert.That(createdWallet, Is.Not.Null, "Wallet should exist in the database");
                Assert.That(createdWallet?.UserID, Is.EqualTo(createWalletDTO.UserID), "UserID should match");
                Assert.That(createdWallet?.WalletName, Is.EqualTo(createWalletDTO.WalletName), "WalletName should match");
                Assert.That(createdWallet?.Balance, Is.EqualTo(createWalletDTO.Balance), "Balance should match");
            });
        }

        [Test]
        public async Task UpdateWalletAsync_ShouldUpdateWallet()
        {
            // Arrange
            var updateWalletDTO = new UpdateWalletDTO
            {
                WalletID = Guid.NewGuid(),
                UserID = Guid.NewGuid().ToString(),
                WalletName = "Updated Wallet",
                Balance = 2000
            };
            var user = new ApplicationUser
            {
                Id = updateWalletDTO.UserID,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = updateWalletDTO.WalletID,
                UserID = updateWalletDTO.UserID,
                WalletName = "Old Wallet",
                Balance = 1000,
                User = user
            };

            context.Users.Add(user);
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.UpdateWalletAsync(updateWalletDTO);

            // Assert
            var updatedWallet = await context.Wallets.FindAsync(result?.WalletID);
            Assert.Multiple(() =>
            {
                Assert.That(updatedWallet, Is.Not.Null, "Wallet should exist in the database");
                Assert.That(updatedWallet?.UserID, Is.EqualTo(updateWalletDTO.UserID), "UserID should match");
                Assert.That(updatedWallet?.WalletName, Is.EqualTo(updateWalletDTO.WalletName), "WalletName should match");
                Assert.That(updatedWallet?.Balance, Is.EqualTo(updateWalletDTO.Balance), "Balance should match");
            });
        }

        [Test]
        public async Task DeleteWalletByIdAsync_ShouldDeleteWallet()
        {
            // Arrange
            var walletId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = walletId,
                UserID = user.Id,
                WalletName = "Test Wallet",
                Balance = 1000,
                User = user
            };

            context.Users.Add(user);
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.DeleteWalletByIdAsync(walletId);

            // Assert
            var deletedWallet = await context.Wallets.FindAsync(result);
            Assert.That(deletedWallet, Is.Null, "Deleted WalletID should not be found");
        }

        [Test]
        public async Task GetAllWalletsAsync_ShouldReturnAllWallets()
        {
            // Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser1",
                Email = "testuser1@example.com"
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser2",
                Email = "testuser2@example.com"
            };
            var wallets = new List<Wallet>
            {
                new Wallet { WalletID = Guid.NewGuid(), UserID = user1.Id, WalletName = "Wallet 1", Balance = 1000, User = user1 },
                new Wallet { WalletID = Guid.NewGuid(), UserID = user2.Id, WalletName = "Wallet 2", Balance = 2000, User = user2 }
            };

            context.Users.AddRange(user1, user2);
            context.Wallets.AddRange(wallets);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.GetAllWalletsAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Result count should be 2");

                var resultList = result.ToList();
                Assert.That(resultList[0].UserID, Is.EqualTo(wallets[0].UserID), "First wallet UserID should match");
                Assert.That(resultList[0].WalletName, Is.EqualTo(wallets[0].WalletName), "First wallet WalletName should match");
                Assert.That(resultList[0].Balance, Is.EqualTo(wallets[0].Balance), "First wallet Balance should match");

                Assert.That(resultList[1].UserID, Is.EqualTo(wallets[1].UserID), "Second wallet UserID should match");
                Assert.That(resultList[1].WalletName, Is.EqualTo(wallets[1].WalletName), "Second wallet WalletName should match");
                Assert.That(resultList[1].Balance, Is.EqualTo(wallets[1].Balance), "Second wallet Balance should match");
            });
        }

        [Test]
        public async Task GetWalletByIdAsync_ShouldReturnWallet()
        {
            // Arrange
            var walletId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = walletId,
                UserID = user.Id,
                WalletName = "Test Wallet",
                Balance = 1000,
                User = user
            };

            context.Users.Add(user);
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.GetWalletByIdAsync(walletId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Wallet should exist in the database");
                Assert.That(result?.UserID, Is.EqualTo(wallet.UserID), "UserID should match");
                Assert.That(result?.WalletName, Is.EqualTo(wallet.WalletName), "WalletName should match");
                Assert.That(result?.Balance, Is.EqualTo(wallet.Balance), "Balance should match");
            });
        }
    }
}
