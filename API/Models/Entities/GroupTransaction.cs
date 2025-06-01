using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class GroupTransaction
    {
        public Guid GroupTransactionID { get; set; }

        public required Guid GroupFundID { get; set; }
        public required GroupFund GroupFund { get; set; }

        public required Guid UserWalletID { get; set; }
        public required Wallet UserWallet { get; set; }

        public required Guid UserCategoryID { get; set; }
        public required Category UserCategory { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public required string Type { get; set; }
    }
}
