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
    public class WalletRepositoryTests
    {
        private ApplicationDbContext context;
        private Mock<IMapper> mapperMock;
        private Mock<ILogger<WalletRepository>> loggerMock;
        private IWalletRepository walletRepository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new ApplicationDbContext(options);

            mapperMock = new Mock<IMapper>();
            loggerMock = new Mock<ILogger<WalletRepository>>();
            walletRepository = new WalletRepository(context, mapperMock.Object, loggerMock.Object);
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
            var walletDTO = new WalletDTO
            {
                WalletID = wallet.WalletID,
                UserID = wallet.UserID,
                WalletName = wallet.WalletName,
                Balance = wallet.Balance
            };

            mapperMock.Setup(m => m.Map<Wallet>(createWalletDTO)).Returns(wallet);
            mapperMock.Setup(m => m.Map<WalletDTO>(wallet)).Returns(walletDTO);

            // Act
            var result = await walletRepository.CreateWalletAsync(createWalletDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.WalletID, Is.EqualTo(walletDTO.WalletID), "WalletID should match");
                Assert.That(result.WalletName, Is.EqualTo(walletDTO.WalletName), "WalletName should match");
                Assert.That(result.Balance, Is.EqualTo(walletDTO.Balance), "Balance should match");
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
            var walletDTO = new WalletDTO
            {
                WalletID = updateWalletDTO.WalletID,
                UserID = updateWalletDTO.UserID,
                WalletName = updateWalletDTO.WalletName,
                Balance = updateWalletDTO.Balance
            };

            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            mapperMock.Setup(m => m.Map<WalletDTO>(wallet)).Returns(walletDTO);

            // Act
            var result = await walletRepository.UpdateWalletAsync(updateWalletDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.WalletID, Is.EqualTo(walletDTO.WalletID), "WalletID should match");
                Assert.That(result.WalletName, Is.EqualTo(walletDTO.WalletName), "WalletName should match");
                Assert.That(result.Balance, Is.EqualTo(walletDTO.Balance), "Balance should match");
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

            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.DeleteWalletByIdAsync(walletId);

            // Assert
            Assert.That(result, Is.EqualTo(walletId), "Deleted WalletID should match");
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
            var walletDTOs = new List<WalletDTO>
            {
                new WalletDTO { WalletID = wallets[0].WalletID, UserID = wallets[0].UserID, WalletName = "Wallet 1", Balance = 1000 },
                new WalletDTO { WalletID = wallets[1].WalletID, UserID = wallets[1].UserID, WalletName = "Wallet 2", Balance = 2000 }
            };

            context.Wallets.AddRange(wallets);
            await context.SaveChangesAsync();

            mapperMock.Setup(m => m.Map<IEnumerable<WalletDTO>>(wallets)).Returns(walletDTOs);

            // Act
            var result = await walletRepository.GetAllWalletsAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Result count should be 2");
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
            var walletDTO = new WalletDTO
            {
                WalletID = walletId,
                UserID = wallet.UserID,
                WalletName = wallet.WalletName,
                Balance = wallet.Balance
            };

            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            mapperMock.Setup(m => m.Map<WalletDTO>(wallet)).Returns(walletDTO);

            // Act
            var result = await walletRepository.GetWalletByIdAsync(walletId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.WalletID, Is.EqualTo(walletDTO.WalletID), "WalletID should match");
                Assert.That(result.WalletName, Is.EqualTo(walletDTO.WalletName), "WalletName should match");
                Assert.That(result.Balance, Is.EqualTo(walletDTO.Balance), "Balance should match");
            });
        }
    }
}
