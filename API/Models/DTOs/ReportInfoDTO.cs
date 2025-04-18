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
}
