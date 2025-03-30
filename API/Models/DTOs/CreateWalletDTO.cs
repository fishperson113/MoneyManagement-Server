using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class CreateWalletDTO
    {
        [Required]
        public string UserID { get; set; } = null!;
        [Required]
        public string WalletName { get; set; } = null!;
        [Required]
        public decimal Balance { get; set; }
    }
}
