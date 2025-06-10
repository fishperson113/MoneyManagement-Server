namespace API.Models.DTOs
{
    public class ReportInfoDTO
    {
        public int ReportId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }    
    public class CreateReportDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Type { get; set; } // e.g., "cash-flow", "category-breakdown","daily-summary", etc.
        public string Format { get; set; } = "pdf"; // default format
        public string Currency { get; set; } = "VND"; // default currency
    }
}
