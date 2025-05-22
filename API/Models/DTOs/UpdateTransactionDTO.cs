using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class UpdateTransactionDTO
    {
        [Required]
        public Guid TransactionID { get; set; }
        [Required]
        public Guid CategoryID { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        [Required]
        public DateTime TransactionDate { get; set; }
        [Required]
        public Guid WalletID { get; set; }

        public required string Type { get; set; }
    }
}
