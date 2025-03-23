namespace API.Models.Entities
{
    public class Transaction
    {
        public Guid TransactionID { get; set; }
        public Guid UserID { get; set; }
        public Guid CategoryID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public required User User { get; set; }
        public required Category Category { get; set; }
    }
}
