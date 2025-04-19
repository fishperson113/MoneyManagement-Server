using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace API.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryRepository(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<CategoryRepository> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User is not authenticated");
        }

        public async Task<CategoryDTO> CreateCategoryAsync(CreateCategoryDTO model)
        {
            try
            {
                _logger.LogInformation("Starting category creation for Name: {CategoryName}", model.Name);

                var category = _mapper.Map<Category>(model);
                category.CategoryID = Guid.NewGuid();
                category.CreatedAt = DateTime.UtcNow;
                category.UserId = GetCurrentUserId(); // Set the current user ID

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

                var userId = GetCurrentUserId();
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryID == model.CategoryID && c.UserId == userId);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryID} not found or doesn't belong to the current user", model.CategoryID);
                    throw new KeyNotFoundException($"Category with ID {model.CategoryID} not found or access denied.");
                }

                category.Name = model.Name;
                // Don't update UserId - preserve the original owner

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

                var userId = GetCurrentUserId();
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryID == model.CategoryID && c.UserId == userId);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryID} not found or doesn't belong to the current user", model.CategoryID);
                    throw new KeyNotFoundException($"Category with ID {model.CategoryID} not found or access denied.");
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
            _logger.LogInformation("Fetching all categories for the current user.");

            try
            {
                var userId = GetCurrentUserId();
                var categories = await _context.Categories
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} categories for user {UserId}.", categories.Count, userId);

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
                var userId = GetCurrentUserId();
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryID == model.CategoryID && c.UserId == userId);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryID} not found or doesn't belong to the current user.", model.CategoryID);
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
