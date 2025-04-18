namespace API.Models.DTOs
{
    public class CategoryBreakdownDTO
    {
        public string Category { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Percentage { get; set; }
    }
}
