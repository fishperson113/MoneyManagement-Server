namespace API.Models.DTOs
{
    public class CategoryBreakdownDTO
    {
        public string Category { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Percentage { get; set; }
        public bool IsIncome { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
    }
}
