namespace API.Models.DTOs
{
    public class WeeklySummaryDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public List<WeeklyDetailDTO> WeeklyDetails { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
        public List<TransactionDetailDTO> Transactions { get; set; } = new List<TransactionDetailDTO>();
        public Dictionary<string, decimal> DailyTotals { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> DailyIncomeTotals { get; set; } = new();
        public Dictionary<string, decimal> DailyExpenseTotals { get; set; } = new();

    }
}