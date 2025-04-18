namespace API.Models.DTOs
{
    public class UpcomingBillDTO
    {
        public int BillId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
    }
}
