using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class CategoryDTO
    {
        public required Guid CategoryID { get; set; }
        public required string Name { get; set; } = null!;
        public required DateTime CreatedAt { get; set; }
    }
}
