namespace API.Models.DTOs
{
    public class TransactionInfo
    {
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? BankName { get; set; }
    }
}
