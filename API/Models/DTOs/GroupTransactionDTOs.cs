namespace API.Models.DTOs
{
    public class GroupTransactionDTO
    {
        public Guid GroupTransactionID { get; set; }
        public Guid GroupFundID { get; set; }
        public Guid UserWalletID { get; set; }
        public Guid UserCategoryID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = string.Empty; // "income" | "expense"
    }

    public class CreateGroupTransactionDTO
    {
        public Guid GroupFundID { get; set; }
        public Guid UserWalletID { get; set; }
        public Guid UserCategoryID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; }
    }

    public class UpdateGroupTransactionDTO
    {
        public Guid GroupTransactionID { get; set; }
        public Guid UserWalletID { get; set; }
        public Guid UserCategoryID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = "expense"; // or "income"
    }
}
