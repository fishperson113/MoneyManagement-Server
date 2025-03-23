namespace API.Models.Entities
{
    public class User
    {
        public Guid UserID { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Category>? Categories { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }
}
