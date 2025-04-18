namespace API.Models.DTOs
{
    public class CashFlowSummaryDTO
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
    }
}
