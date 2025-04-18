using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class CreateTransactionDTO
    {
        [Required]
        public Guid CategoryID { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        [Required]
        public DateTime TransactionDate { get; set; }
        [Required]
        public Guid WalletID { get; set; }

        public string? Type { get; set; }
    }
}
