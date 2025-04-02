using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryRepository categoryRepository, ILogger<CategoriesController> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        // GET: api/<CategoriesController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetAllCategories()
        {
            try
            {
                var categories = await _categoryRepository.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all categories");
                return StatusCode(500, "An error occurred while retrieving categories");
            }
        }

        // GET api/<CategoriesController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDTO>> GetCategoryById(Guid id)
        {
            try
            {
                var getCategoryDTO = new GetCategoryByIdDTO { CategoryID = id };
                var category = await _categoryRepository.GetCategoryByIdAsync(getCategoryDTO);

                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving category with ID {CategoryId}", id);
                return StatusCode(500, "An error occurred while retrieving the category");
            }
        }

        // POST api/<CategoriesController>
        [HttpPost]
        public async Task<ActionResult<CategoryDTO>> CreateCategory([FromBody] CreateCategoryDTO createCategoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdCategory = await _categoryRepository.CreateCategoryAsync(createCategoryDTO);
                return CreatedAtAction(nameof(GetCategoryById), new { id = createdCategory.CategoryID }, createdCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a category");
                return StatusCode(500, "An error occurred while creating the category");
            }
        }

        // PUT api/<CategoriesController>
        [HttpPut]
        public async Task<ActionResult<CategoryDTO>> UpdateCategory([FromBody] UpdateCategoryDTO updateCategoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedCategory = await _categoryRepository.UpdateCategoryAsync(updateCategoryDTO);
                return Ok(updatedCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category with ID {CategoryId}", updateCategoryDTO.CategoryID);
                return StatusCode(500, "An error occurred while updating the category");
            }
        }

        // DELETE api/<CategoriesController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var deleteCategoryDTO = new DeleteCategoryByIdDTO { CategoryID = id };
                await _categoryRepository.DeleteCategoryByIdAsync(deleteCategoryDTO);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category with ID {CategoryId}", id);
                return StatusCode(500, "An error occurred while deleting the category");
            }
        }
    }
}
