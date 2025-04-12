using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(ApplicationDbContext context, IMapper mapper, ILogger<CategoryRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CategoryDTO> CreateCategoryAsync(CreateCategoryDTO model)
        {
            try
            {
                _logger.LogInformation("Starting category creation for Name: {CategoryName}", model.Name);

                var category = _mapper.Map<Category>(model);
                category.CategoryID = Guid.NewGuid();
                category.CreatedAt = DateTime.UtcNow;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();          

                _logger.LogInformation("Category created successfully with ID: {CategoryID}", category.CategoryID);
                return _mapper.Map<CategoryDTO>(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category: {CategoryName}", model.Name);
                throw;
            }
        }

        public async Task<CategoryDTO> UpdateCategoryAsync(UpdateCategoryDTO model)
        {
            try
            {
                _logger.LogInformation("Updating category with ID: {CategoryID}", model.CategoryID);

                var category = await _context.Categories.FindAsync(model.CategoryID);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryID} not found", model.CategoryID);
                    throw new KeyNotFoundException($"Category with ID {model.CategoryID} not found.");
                }

                category.Name = model.Name;

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category with ID {CategoryID} updated successfully", category.CategoryID);
                return _mapper.Map<CategoryDTO>(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category: {CategoryID}", model.CategoryID);
                throw;
            }
        }

        public async Task<Guid> DeleteCategoryByIdAsync(DeleteCategoryByIdDTO model)
        {
            try
            {
                _logger.LogInformation("Deleting category with ID: {CategoryID}", model.CategoryID);

                var category = await _context.Categories.FindAsync(model.CategoryID);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryID} not found", model.CategoryID);
                    throw new KeyNotFoundException($"Category with ID {model.CategoryID} not found.");
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category with ID {CategoryID} deleted successfully", model.CategoryID);
                return model.CategoryID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category: {CategoryID}", model.CategoryID);
                throw;
            }
        }

        public async Task<IEnumerable<CategoryDTO>> GetAllCategoriesAsync()
        {
            _logger.LogInformation("Fetching all categories from the database.");

            try
            {
                var categories = await _context.Categories.ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} categories.", categories.Count);

                return _mapper.Map<IEnumerable<CategoryDTO>>(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching categories.");
                throw;
            }
        }

        public async Task<CategoryDTO?> GetCategoryByIdAsync(GetCategoryByIdDTO model)
        {
            _logger.LogInformation("Fetching category with ID: {CategoryID}", model.CategoryID);

            try
            {
                var category = await _context.Categories.FindAsync(model.CategoryID);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryID} not found.", model.CategoryID);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved category with ID: {CategoryID}", model.CategoryID);
                return _mapper.Map<CategoryDTO>(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching category with ID: {CategoryID}", model.CategoryID);
                throw;
            }
        }

    }
}
