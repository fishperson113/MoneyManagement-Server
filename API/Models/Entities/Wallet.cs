namespace API.Models.Entities
{
    public class Wallet
    {
        public Guid WalletID { get; set; }
        public required string WalletName { get; set; }
        public decimal Balance { get; set; }
        public string? UserId { get; set; }  
        public ApplicationUser? User { get; set; } 
        public ICollection<Transaction>? Transactions { get; set; }
    }
}
