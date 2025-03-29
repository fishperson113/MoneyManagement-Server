using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class CreateWalletDTO
    {
        [Required]
        public string UserID { get; set; }
        [Required]
        public string WalletName { get; set; }
        [Required]
        public decimal Balance { get; set; }
    }
}
