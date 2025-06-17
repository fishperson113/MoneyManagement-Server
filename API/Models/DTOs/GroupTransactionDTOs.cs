namespace API.Models.DTOs
{
    public class GroupTransactionDTO
    {
        public Guid GroupTransactionID { get; set; }
        public Guid GroupFundID { get; set; }
        
        // Optional personal tracking fields
        public Guid? UserWalletID { get; set; }
        public string? UserWalletName { get; set; }
        public Guid? UserCategoryID { get; set; }
        public string? UserCategoryName { get; set; }
        
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = string.Empty; // "income" | "expense"
        
        // Tracking info
        public string CreatedByUserId { get; set; } = string.Empty;
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateGroupTransactionDTO
    {
        public Guid GroupFundID { get; set; }
        
        // Optional: Link to personal wallet/category for tracking
        public Guid? UserWalletID { get; set; }
        public Guid? UserCategoryID { get; set; }
        
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string Type { get; set; } = "expense";
    }

    public class UpdateGroupTransactionDTO
    {
        public Guid GroupTransactionID { get; set; }
        
        // Optional: Link to personal wallet/category for tracking
        public Guid? UserWalletID { get; set; }
        public Guid? UserCategoryID { get; set; }
        
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = "expense"; // or "income"
    }
}
