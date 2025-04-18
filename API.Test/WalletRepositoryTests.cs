using API.Data;
using API.Helpers;
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        private Mock<IHttpContextAccessor> httpContextAccessorMock;
        private string testUserId = "test-user-id";

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

            // Set up HttpContextAccessor mock
            httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, testUserId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            walletRepository = new WalletRepository(context, mapper, logger, httpContextAccessorMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            context?.Dispose();
        }

        [Test]
        public async Task CreateWalletAsync_ShouldCreateWalletWithCorrectUserId()
        {
            // Arrange
            var createWalletDTO = new CreateWalletDTO
            {
                WalletName = "Test Wallet",
                Balance = 1000
            };
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser",
                Email = "testuser@example.com"
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
                Assert.That(createdWallet?.WalletName, Is.EqualTo(createWalletDTO.WalletName), "WalletName should match");
                Assert.That(createdWallet?.Balance, Is.EqualTo(createWalletDTO.Balance), "Balance should match");
                Assert.That(createdWallet?.UserId, Is.EqualTo(testUserId), "UserId should match the current user");
            });
        }

        [Test]
        public async Task UpdateWalletAsync_ShouldUpdateWalletOwnedByUser()
        {
            // Arrange
            var updateWalletDTO = new UpdateWalletDTO
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Updated Wallet",
                Balance = 2000
            };
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = updateWalletDTO.WalletID,
                WalletName = "Old Wallet",
                Balance = 1000,
                UserId = testUserId // Important: associate with test user
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
                Assert.That(updatedWallet?.WalletName, Is.EqualTo(updateWalletDTO.WalletName), "WalletName should match");
                Assert.That(updatedWallet?.Balance, Is.EqualTo(updateWalletDTO.Balance), "Balance should match");
                Assert.That(updatedWallet?.UserId, Is.EqualTo(testUserId), "UserId should still match the current user");
            });
        }

        [Test]
        public void UpdateWalletAsync_ShouldThrowForWalletNotOwnedByUser()
        {
            // Arrange
            var updateWalletDTO = new UpdateWalletDTO
            {
                WalletID = Guid.NewGuid(),
                WalletName = "Updated Wallet",
                Balance = 2000
            };
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var otherUser = new ApplicationUser
            {
                Id = "other-user-id",
                UserName = "otheruser",
                Email = "otheruser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = updateWalletDTO.WalletID,
                WalletName = "Old Wallet",
                Balance = 1000,
                UserId = "other-user-id" // Wallet owned by another user
            };

            context.Users.AddRange(user, otherUser);
            context.Wallets.Add(wallet);
            context.SaveChanges();

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(() => walletRepository.UpdateWalletAsync(updateWalletDTO));
        }

        [Test]
        public async Task DeleteWalletByIdAsync_ShouldDeleteWalletOwnedByUser()
        {
            // Arrange
            var walletId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = walletId,
                WalletName = "Test Wallet",
                Balance = 1000,
                UserId = testUserId // Important: associate with test user
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
        public void DeleteWalletByIdAsync_ShouldThrowForWalletNotOwnedByUser()
        {
            // Arrange
            var walletId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var otherUser = new ApplicationUser
            {
                Id = "other-user-id",
                UserName = "otheruser",
                Email = "otheruser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = walletId,
                WalletName = "Test Wallet",
                Balance = 1000,
                UserId = "other-user-id" // Wallet owned by another user
            };

            context.Users.AddRange(user, otherUser);
            context.Wallets.Add(wallet);
            context.SaveChanges();

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(() => walletRepository.DeleteWalletByIdAsync(walletId));
        }

        [Test]
        public async Task GetAllWalletsAsync_ShouldReturnOnlyUserOwnedWallets()
        {
            // Arrange
            var user1 = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser1",
                Email = "testuser1@example.com"
            };
            var user2 = new ApplicationUser
            {
                Id = "other-user-id",
                UserName = "testuser2",
                Email = "testuser2@example.com"
            };
            var wallets = new List<Wallet>
            {
                new Wallet { WalletID = Guid.NewGuid(), WalletName = "User1 Wallet 1", Balance = 1000, UserId = testUserId },
                new Wallet { WalletID = Guid.NewGuid(), WalletName = "User1 Wallet 2", Balance = 2000, UserId = testUserId },
                new Wallet { WalletID = Guid.NewGuid(), WalletName = "User2 Wallet", Balance = 3000, UserId = "other-user-id" }
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
                Assert.That(result.Count(), Is.EqualTo(2), "Should only return wallets for the current user");

                var resultList = result.ToList();
                Assert.That(resultList.Any(w => w.WalletName == "User2 Wallet"), Is.False, "Should not return wallets from other users");
                Assert.That(resultList.Any(w => w.WalletName == "User1 Wallet 1"), Is.True, "Should return the user's first wallet");
                Assert.That(resultList.Any(w => w.WalletName == "User1 Wallet 2"), Is.True, "Should return the user's second wallet");
            });
        }

        [Test]
        public async Task GetWalletByIdAsync_ShouldReturnWalletOwnedByUser()
        {
            // Arrange
            var walletId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = walletId,
                WalletName = "Test Wallet",
                Balance = 1000,
                UserId = testUserId // Important: associate with test user
            };

            context.Users.Add(user);
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.GetWalletByIdAsync(walletId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Wallet should exist and be accessible");
                Assert.That(result?.WalletName, Is.EqualTo(wallet.WalletName), "WalletName should match");
                Assert.That(result?.Balance, Is.EqualTo(wallet.Balance), "Balance should match");
            });
        }

        [Test]
        public async Task GetWalletByIdAsync_ShouldReturnNullForWalletNotOwnedByUser()
        {
            // Arrange
            var walletId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "testuser",
                Email = "testuser@example.com"
            };
            var otherUser = new ApplicationUser
            {
                Id = "other-user-id",
                UserName = "otheruser",
                Email = "otheruser@example.com"
            };
            var wallet = new Wallet
            {
                WalletID = walletId,
                WalletName = "Test Wallet",
                Balance = 1000,
                UserId = "other-user-id" // Wallet owned by another user
            };

            context.Users.AddRange(user, otherUser);
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act
            var result = await walletRepository.GetWalletByIdAsync(walletId);

            // Assert
            Assert.That(result, Is.Null, "Should not return wallets owned by other users");
        }
    }
}
