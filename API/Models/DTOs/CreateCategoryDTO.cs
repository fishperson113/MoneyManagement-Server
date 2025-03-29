using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class CreateCategoryDTO
    {
        [Required]
        public string Name { get; set; } = null!;
    }
}
