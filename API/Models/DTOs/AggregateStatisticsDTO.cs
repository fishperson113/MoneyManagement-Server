namespace API.Models.DTOs
{
    public class AggregateStatisticsDTO
    {
        public string Period { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}