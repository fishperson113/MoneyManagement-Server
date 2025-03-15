namespace API.Models.Entities
{
    public class User
    {
        public Guid ID { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
        public string? Email { get; set; }
    }
}
