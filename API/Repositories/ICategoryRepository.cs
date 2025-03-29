using API.Models.DTOs;
using API.Models.Entities;

namespace API.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<CategoryDTO>> GetAllCategoriesAsync();
        Task<CategoryDTO?> GetCategoryByIdAsync(GetCategoryByIdDTO model);
        Task<CategoryDTO> CreateCategoryAsync(CreateCategoryDTO model);
        Task<CategoryDTO> UpdateCategoryAsync(UpdateCategoryDTO model);
        Task<Guid> DeleteCategoryByIdAsync(DeleteCategoryByIdDTO model);
    }
}
