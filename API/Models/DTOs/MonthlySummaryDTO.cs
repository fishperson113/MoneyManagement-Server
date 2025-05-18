namespace API.Models.DTOs
{
    public class MonthlySummaryDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public List<MonthlyDetailDTO> MonthlyDetails { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
        public List<TransactionDetailDTO> Transactions { get; set; } = new List<TransactionDetailDTO>();
        public Dictionary<int, decimal> DailyTotals { get; set; } = new Dictionary<int, decimal>();
        public Dictionary<string, decimal> CategoryTotals { get; set; } = new Dictionary<string, decimal>();

    }
}