namespace API.Models.DTOs
{
    public class UpdateUserDTO
    {
        public required string Name { get; set; }
        public required string Password { get; set; }
        public string? Email { get; set; }
    }
}
