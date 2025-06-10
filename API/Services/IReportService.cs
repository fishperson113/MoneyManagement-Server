using API.Models.DTOs;

namespace API.Services;

/// <summary>
/// Report generation service interface following the Template Method pattern
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Main report generation method following the described pattern
    /// </summary>
    /// <param name="reportInfo">Report configuration</param>
    /// <returns>Report information with download details</returns>
    Task<ReportInfoDTO> GenerateReportAsync(CreateReportDTO reportInfo);
}
