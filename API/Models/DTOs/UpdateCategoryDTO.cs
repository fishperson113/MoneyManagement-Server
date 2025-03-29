using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class UpdateCategoryDTO
    {
        public Guid CategoryID { get; set; } // No [Required] needed for Guid

        [Required]
        public string Name { get; set; } = null!;
    }
}
