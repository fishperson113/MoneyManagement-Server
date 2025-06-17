using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class GroupTransaction
    {
        public Guid GroupTransactionID { get; set; }

        public required Guid GroupFundID { get; set; }
        public required GroupFund GroupFund { get; set; }

        // Optional: Link to user's personal wallet for tracking
        public Guid? UserWalletID { get; set; }
        public Wallet? UserWallet { get; set; }

        // Optional: Link to user's personal category for tracking
        public Guid? UserCategoryID { get; set; }
        public Category? UserCategory { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public required string Type { get; set; }

        // Track who created this transaction
        public required string CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
