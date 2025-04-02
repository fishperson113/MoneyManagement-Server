using System;

namespace API.Models.DTOs
{
    public class WalletDTO
    {
        public Guid WalletID { get; set; }
        public string UserID { get; set; } = null!;
        public string WalletName { get; set; } = null!;
        public decimal Balance { get; set; }
    }
}
