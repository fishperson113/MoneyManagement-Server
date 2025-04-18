using System;

namespace API.Models.DTOs
{
    public class TransactionDTO
    {
        public Guid TransactionID { get; set; }
        public Guid CategoryID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid WalletID { get; set; }
    }
}
