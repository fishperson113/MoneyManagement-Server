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
    public class CategoryRepositoryTests
    {
        private ApplicationDbContext context;
        private Mock<IMapper> mapperMock;
        private Mock<ILogger<CategoryRepository>> loggerMock;
        private ICategoryRepository categoryRepository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new ApplicationDbContext(options);

            mapperMock = new Mock<IMapper>();
            loggerMock = new Mock<ILogger<CategoryRepository>>();
            categoryRepository = new CategoryRepository(context, mapperMock.Object, loggerMock.Object);
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
            var category = new Category { CategoryID = Guid.NewGuid(), Name = "Test Category", CreatedAt = DateTime.UtcNow };
            var categoryDTO = new CategoryDTO { CategoryID = category.CategoryID, Name = "Test Category", CreatedAt = DateTime.UtcNow };

            mapperMock.Setup(m => m.Map<Category>(createCategoryDTO)).Returns(category);
            mapperMock.Setup(m => m.Map<CategoryDTO>(category)).Returns(categoryDTO);

            // Act
            var result = await categoryRepository.CreateCategoryAsync(createCategoryDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result!.CategoryID, Is.EqualTo(categoryDTO.CategoryID), "CategoryID should match");
                Assert.That(result.Name, Is.EqualTo(categoryDTO.Name), "Name should match");
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

            mapperMock.Setup(m => m.Map<CategoryDTO>(category)).Returns(categoryDTO);

            // Act
            var result = await categoryRepository.UpdateCategoryAsync(updateCategoryDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result!.CategoryID, Is.EqualTo(categoryDTO.CategoryID), "CategoryID should match");
                Assert.That(result.Name, Is.EqualTo(categoryDTO.Name), "Name should match");
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
            Assert.That(result, Is.EqualTo(deleteCategoryByIdDTO.CategoryID), "Deleted CategoryID should match");
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
            var categoryDTOs = new List<CategoryDTO>
                    {
                        new CategoryDTO { CategoryID = categories[0].CategoryID, Name = "Category 1", CreatedAt = DateTime.UtcNow },
                        new CategoryDTO { CategoryID = categories[1].CategoryID, Name = "Category 2", CreatedAt = DateTime.UtcNow }
                    };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            mapperMock.Setup(m => m.Map<IEnumerable<CategoryDTO>>(categories)).Returns(categoryDTOs);

            // Act
            var result = await categoryRepository.GetAllCategoriesAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result.Count(), Is.EqualTo(2), "Result count should be 2");
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

            mapperMock.Setup(m => m.Map<CategoryDTO>(category)).Returns(categoryDTO);

            // Act
            var result = await categoryRepository.GetCategoryByIdAsync(getCategoryByIdDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result should not be null");
                Assert.That(result!.CategoryID, Is.EqualTo(categoryDTO.CategoryID), "CategoryID should match");
                Assert.That(result.Name, Is.EqualTo(categoryDTO.Name), "Name should match");
            });
        }
    }
}
