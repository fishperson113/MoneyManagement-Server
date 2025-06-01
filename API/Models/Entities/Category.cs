namespace API.Models.Entities
{
    public class Category
    {
        public Guid CategoryID { get; set; }
        public required string Name { get; set; }
        public required DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
        public ICollection<GroupTransaction> GroupTransactions { get; set; } = new List<GroupTransaction>();

    }
}
