﻿namespace API.Models.Entities
{
    public class Wallet
    {
        public Guid WalletID { get; set; }
        public required string WalletName { get; set; }
        public decimal Balance { get; set; }
        public required string UserId { get; set; }  
        public required ApplicationUser? User { get; set; } 
        public ICollection<Transaction>? Transactions { get; set; }
        public ICollection<GroupTransaction> GroupTransactions { get; set; } = new List<GroupTransaction>();

    }
}
