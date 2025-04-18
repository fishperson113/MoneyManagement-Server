namespace API.Models.DTOs
{
    public class TransactionDetailDTO
    {
        public Guid TransactionID { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid WalletID { get; set; }
        public string WalletName { get; set; } = string.Empty;
    }
}
