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
    public class CategoryRepositoryTests
    {
        private ApplicationDbContext context;
        private IMapper mapper;
        private ILogger<CategoryRepository> logger;
        private ICategoryRepository categoryRepository;
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
            logger = loggerFactory.CreateLogger<CategoryRepository>();

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

            categoryRepository = new CategoryRepository(context, mapper, logger, httpContextAccessorMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            context?.Dispose();
        }

        [Test]
        public async Task CreateCategoryAsync_ShouldCreateCategoryWithUserId()
        {
            // Arrange
            var createCategoryDTO = new CreateCategoryDTO { Name = "Test Category" };

            // Act
            var result = await categoryRepository.CreateCategoryAsync(createCategoryDTO);

            // Assert
            var createdCategory = await context.Categories.FindAsync(result?.CategoryID);
            Assert.Multiple(() =>
            {
                Assert.That(createdCategory, Is.Not.Null, "Category should exist in the database");
                Assert.That(createdCategory?.Name, Is.EqualTo(createCategoryDTO.Name), "Name in DB should match the input Name");
                Assert.That(createdCategory?.UserId, Is.EqualTo(testUserId), "UserId should match the test user ID");
            });
        }

        [Test]
        public async Task UpdateCategoryAsync_ShouldUpdateCategoryOwnedByUser()
        {
            // Arrange
            var updateCategoryDTO = new UpdateCategoryDTO { CategoryID = Guid.NewGuid(), Name = "Updated Category" };
            var category = new Category
            {
                CategoryID = updateCategoryDTO.CategoryID,
                Name = "Old Category",
                CreatedAt = DateTime.UtcNow,
                UserId = testUserId // Important: associate with test user
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.UpdateCategoryAsync(updateCategoryDTO);

            // Assert
            var updatedCategory = await context.Categories.FindAsync(result?.CategoryID);
            Assert.Multiple(() =>
            {
                Assert.That(updatedCategory, Is.Not.Null, "Category should exist in the database");
                Assert.That(updatedCategory?.Name, Is.EqualTo(updateCategoryDTO.Name), "Name should match");
                Assert.That(updatedCategory?.UserId, Is.EqualTo(testUserId), "UserId should not change");
            });
        }

        [Test]
        public void UpdateCategoryAsync_ShouldThrowForCategoryNotOwnedByUser()
        {
            // Arrange
            var updateCategoryDTO = new UpdateCategoryDTO { CategoryID = Guid.NewGuid(), Name = "Updated Category" };
            var category = new Category
            {
                CategoryID = updateCategoryDTO.CategoryID,
                Name = "Old Category",
                CreatedAt = DateTime.UtcNow,
                UserId = "other-user-id" // Owned by another user
            };

            context.Categories.Add(category);
            context.SaveChanges();

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(() => categoryRepository.UpdateCategoryAsync(updateCategoryDTO));
        }

        [Test]
        public async Task DeleteCategoryByIdAsync_ShouldDeleteCategoryOwnedByUser()
        {
            // Arrange
            var deleteCategoryByIdDTO = new DeleteCategoryByIdDTO { CategoryID = Guid.NewGuid() };
            var category = new Category
            {
                CategoryID = deleteCategoryByIdDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow,
                UserId = testUserId // Important: associate with test user
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.DeleteCategoryByIdAsync(deleteCategoryByIdDTO);

            // Assert
            var deletedCategory = await context.Categories.FindAsync(result);
            Assert.That(deletedCategory, Is.Null, "Deleted CategoryID should not be found");
        }

        [Test]
        public void DeleteCategoryByIdAsync_ShouldThrowForCategoryNotOwnedByUser()
        {
            // Arrange
            var deleteCategoryByIdDTO = new DeleteCategoryByIdDTO { CategoryID = Guid.NewGuid() };
            var category = new Category
            {
                CategoryID = deleteCategoryByIdDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow,
                UserId = "other-user-id" // Owned by another user
            };

            context.Categories.Add(category);
            context.SaveChanges();

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(() => categoryRepository.DeleteCategoryByIdAsync(deleteCategoryByIdDTO));
        }

        [Test]
        public async Task GetAllCategoriesAsync_ShouldReturnOnlyUserOwnedCategories()
        {
            // Arrange
            var userCategories = new List<Category>
            {
                new Category { CategoryID = Guid.NewGuid(), Name = "User Category 1", CreatedAt = DateTime.UtcNow, UserId = testUserId },
                new Category { CategoryID = Guid.NewGuid(), Name = "User Category 2", CreatedAt = DateTime.UtcNow, UserId = testUserId }
            };

            var otherUserCategories = new List<Category>
            {
                new Category { CategoryID = Guid.NewGuid(), Name = "Other User Category", CreatedAt = DateTime.UtcNow, UserId = "other-user-id" }
            };

            context.Categories.AddRange(userCategories);
            context.Categories.AddRange(otherUserCategories);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.GetAllCategoriesAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Result should only contain the current user's categories");

                var resultList = result.ToList();
                Assert.That(resultList.Any(c => c.Name == "Other User Category"), Is.False, "Should not include other user's categories");
                Assert.That(resultList.Any(c => c.Name == "User Category 1"), Is.True, "Should include user's first category");
                Assert.That(resultList.Any(c => c.Name == "User Category 2"), Is.True, "Should include user's second category");
            });
        }

        [Test]
        public async Task GetCategoryByIdAsync_ShouldReturnCategoryOwnedByUser()
        {
            // Arrange
            var getCategoryByIdDTO = new GetCategoryByIdDTO { CategoryID = Guid.NewGuid() };
            var category = new Category
            {
                CategoryID = getCategoryByIdDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow,
                UserId = testUserId // Important: associate with test user
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.GetCategoryByIdAsync(getCategoryByIdDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result?.CategoryID, Is.EqualTo(category.CategoryID), "CategoryID should match");
                Assert.That(result?.Name, Is.EqualTo(category.Name), "Name should match");
            });
        }

        [Test]
        public async Task GetCategoryByIdAsync_ShouldReturnNullForCategoryNotOwnedByUser()
        {
            // Arrange
            var getCategoryByIdDTO = new GetCategoryByIdDTO { CategoryID = Guid.NewGuid() };
            var category = new Category
            {
                CategoryID = getCategoryByIdDTO.CategoryID,
                Name = "Test Category",
                CreatedAt = DateTime.UtcNow,
                UserId = "other-user-id" // Owned by another user
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.GetCategoryByIdAsync(getCategoryByIdDTO);

            // Assert
            Assert.That(result, Is.Null, "Should not return categories owned by other users");
        }
    }
}
