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
    public class CategoryRepositoryTests
    {
        private ApplicationDbContext context;
        private IMapper mapper;
        private ILogger<CategoryRepository> logger;
        private ICategoryRepository categoryRepository;

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

            categoryRepository = new CategoryRepository(context, mapper, logger);
        }

        [TearDown]
        public void TearDown()
        {
            context?.Dispose();
        }

        [Test]
        public async Task CreateCategoryAsync_ShouldCreateCategory()
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
            });
        }

        [Test]
        public async Task UpdateCategoryAsync_ShouldUpdateCategory()
        {
            // Arrange
            var updateCategoryDTO = new UpdateCategoryDTO { CategoryID = Guid.NewGuid(), Name = "Updated Category" };
            var category = new Category { CategoryID = updateCategoryDTO.CategoryID, Name = "Old Category", CreatedAt = DateTime.UtcNow };
            var categoryDTO = new CategoryDTO { CategoryID = updateCategoryDTO.CategoryID, Name = "Updated Category", CreatedAt = DateTime.UtcNow };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.UpdateCategoryAsync(updateCategoryDTO);

            // Assert
            var updatedCategory = await context.Categories.FindAsync(result?.CategoryID);
            Assert.Multiple(() =>
            {
                Assert.That(updatedCategory, Is.Not.Null, "Result should not be null");
                Assert.That(updatedCategory?.CategoryID, Is.EqualTo(categoryDTO.CategoryID), "CategoryID should match");
                Assert.That(updatedCategory?.Name, Is.EqualTo(categoryDTO.Name), "Name should match");
            });
        }

        [Test]
        public async Task DeleteCategoryByIdAsync_ShouldDeleteCategory()
        {
            // Arrange
            var deleteCategoryByIdDTO = new DeleteCategoryByIdDTO { CategoryID = Guid.NewGuid() };
            var category = new Category { CategoryID = deleteCategoryByIdDTO.CategoryID, Name = "Test Category", CreatedAt = DateTime.UtcNow };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.DeleteCategoryByIdAsync(deleteCategoryByIdDTO);

            // Assert
            var deletedCategory = await context.Categories.FindAsync(result);
            Assert.That(deletedCategory, Is.Null, "Deleted CategoryID should not be found");
        }
        [Test]
        public async Task GetAllCategoriesAsync_ShouldReturnAllCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryID = Guid.NewGuid(), Name = "Category 1", CreatedAt = DateTime.UtcNow },
                new Category { CategoryID = Guid.NewGuid(), Name = "Category 2", CreatedAt = DateTime.UtcNow }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.GetAllCategoriesAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Result count should be 2");

                var resultList = result.ToList();
                Assert.That(resultList[0].CategoryID, Is.EqualTo(categories[0].CategoryID), "First category ID should match");
                Assert.That(resultList[1].CategoryID, Is.EqualTo(categories[1].CategoryID), "Second category ID should match");
            });
        }


        [Test]
        public async Task GetCategoryByIdAsync_ShouldReturnCategory()
        {
            // Arrange
            var getCategoryByIdDTO = new GetCategoryByIdDTO { CategoryID = Guid.NewGuid() };
            var category = new Category { CategoryID = getCategoryByIdDTO.CategoryID, Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var categoryDTO = new CategoryDTO { CategoryID = getCategoryByIdDTO.CategoryID, Name = "Test Category", CreatedAt = DateTime.UtcNow };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await categoryRepository.GetCategoryByIdAsync(getCategoryByIdDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result?.CategoryID, Is.EqualTo(categoryDTO.CategoryID), "CategoryID should match");
                Assert.That(result?.Name, Is.EqualTo(categoryDTO.Name), "Name should match");
            });
        }
    }
}

