﻿namespace API.Models.Entities
{
    public class Category
    {
        public Guid CategoryID { get; set; }
        public Guid UserID { get; set; }
        public required string Name { get; set; }
        public required string Type { get; set; } //Income/Outcome
        public DateTime CreatedAt { get; set; }
        public required User User { get; set; }
        public ICollection<Transaction>? Transactions { get; set; } 
    }
}
