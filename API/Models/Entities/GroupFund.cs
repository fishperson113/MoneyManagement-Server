using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class GroupFund
    {
        [Key]
        public Guid GroupFundID { get; set; }

        [Required]
        public Guid GroupID { get; set; }
        public Group? Group { get; set; }
        public string? Description { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalFundsIn { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalFundsOut { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        //public decimal SavingGoal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Transaction>? Transactions { get; set; }
    }
}
