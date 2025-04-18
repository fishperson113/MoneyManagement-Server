namespace API.Models.DTOs
{
    public class DailySummaryDTO
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public List<TransactionDetailDTO> Transactions { get; set; } = new List<TransactionDetailDTO>();

    }
}
