namespace API.Models.DTOs
{
    public class CategoryBreakdownDTO
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal IncomePercentage { get; set; }
        public decimal ExpensePercentage { get; set; }

    }
}
