namespace API.Models.DTOs
{
    public class GroupFundDTO
    {
        public Guid GroupFundID { get; set; }
        public Guid GroupID { get; set; }

        public string? Description { get; set; }
        public decimal TotalFundsIn { get; set; }
        public decimal TotalFundsOut { get; set; }
        public decimal Balance { get; set; }
        public decimal SavingGoal { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateGroupFundDTO
    {
        public Guid GroupID { get; set; }
        public string? Description { get; set; }
        public decimal SavingGoal { get; set; }

    }

    public class UpdateGroupFundDTO
    {
        public Guid GroupFundID { get; set; }
        public string? Description { get; set; }
        public decimal SavingGoal { get; set; }
    }

    public class DeleteGroupFundByIdDTO
    {
        public Guid GroupFundID { get; set; }
    }    
    
    public class GetGroupFundByGroupIdDTO
    {
        public Guid GroupID { get; set; }
    }

    /// <summary>
    /// DTO for broadcasting GroupFund updates via SignalR
    /// </summary>
    public class GroupFundUpdateNotificationDTO
    {
        /// <summary>
        /// The ID of the GroupFund that was updated
        /// </summary>
        public Guid GroupFundID { get; set; }

        /// <summary>
        /// The ID of the Group that owns the fund
        /// </summary>
        public Guid GroupID { get; set; }

        /// <summary>
        /// The updated balance after the transaction
        /// </summary>
        public decimal NewBalance { get; set; }

        /// <summary>
        /// The total funds received (income)
        /// </summary>
        public decimal TotalFundsIn { get; set; }

        /// <summary>
        /// The total funds spent (expenses)
        /// </summary>
        public decimal TotalFundsOut { get; set; }        
        /// <summary>
        /// The ID of the GROUP TRANSACTION that caused this fund update
        /// Note: This is a GroupTransaction ID, not a regular Transaction ID
        /// </summary>
        public Guid TransactionID { get; set; }

        /// <summary>
        /// The type of GROUP TRANSACTION ('income' or 'expense')
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// The amount of the transaction
        /// </summary>
        public decimal TransactionAmount { get; set; }

        /// <summary>
        /// Description of the transaction
        /// </summary>
        public string? TransactionDescription { get; set; }

        /// <summary>
        /// When the update occurred
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The user who created the transaction
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}
