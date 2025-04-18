using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class UpdateWalletDTO
    {
        [Required]
        public Guid WalletID { get; set; }
        [Required]
        public string WalletName { get; set; } = null!;
        [Required]
        public decimal Balance { get; set; }
    }
}
