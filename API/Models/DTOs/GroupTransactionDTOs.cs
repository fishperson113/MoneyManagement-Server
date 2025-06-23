using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// DTO for retrieving group transaction comments
    /// </summary>
    public class GroupTransactionCommentDTO
    {
        public Guid CommentId { get; set; }
        public Guid GroupTransactionId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? UserAvatarUrl { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new group transaction comment
    /// </summary>
    public class CreateGroupTransactionCommentDTO
    {
        public required Guid GroupTransactionId { get; set; }
        public required string Content { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing group transaction comment
    /// </summary>
    public class UpdateGroupTransactionCommentDTO
    {
        public required Guid CommentId { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public required string Content { get; set; }
    }

    /// <summary>
    /// DTO for deleting a group transaction comment
    /// </summary>
    public class DeleteGroupTransactionCommentDTO
    {
        public required Guid CommentId { get; set; }
    }
}
