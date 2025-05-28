namespace API.Models.DTOs
{
    public class GroupFundDTO
    {
        public Guid GroupFundID { get; set; }
        public Guid GroupID { get; set; }

        public decimal TotalFundsIn { get; set; }
        public decimal TotalFundsOut { get; set; }
        public decimal Balance { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<TransactionDTO>? Transactions { get; set; }
    }

    public class CreateGroupFundDTO
    {
        public Guid GroupID { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateGroupFundDTO
    {
        public Guid GroupID { get; set; }
        public Guid GroupFundID { get; set; }
        public string? Description { get; set; }
    }

    public class DeleteGroupFundByIdDTO
    {
        public Guid GroupID { get; set; }
        public Guid GroupFundID { get; set; }
    }

    public class GetGroupFundByGroupIdDTO
    {
        public Guid GroupID { get; set; }
    }
}
