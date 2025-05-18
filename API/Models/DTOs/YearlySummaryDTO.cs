namespace API.Models.DTOs
{
    public class YearlySummaryDTO
    {
        public int Year { get; set; }
        public List<YearlyDetailDTO> YearlyDetails { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
        public List<TransactionDetailDTO> Transactions { get; set; } = new List<TransactionDetailDTO>();
        public Dictionary<string, decimal> MonthlyTotals { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> CategoryTotals { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> QuarterlyTotals { get; set; } = new Dictionary<string, decimal>();

    }
}