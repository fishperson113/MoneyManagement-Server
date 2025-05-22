using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class Transaction
    {
        public Guid TransactionID { get; set; }
        public Guid CategoryID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public Guid WalletID { get; set; }
        public required string Type { get; set; }
        public required Wallet Wallet { get; set; }
        public required Category Category { get; set; }
    }
}
