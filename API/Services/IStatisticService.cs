using API.Models.DTOs;

namespace API.Services;

/// <summary>
/// Statistics service interface for generating report data
/// </summary>
public interface IStatisticService
{
    /// <summary>
    /// Generates raw report data based on configuration
    /// </summary>
    /// <param name="reportInfo">Report configuration</param>
    /// <returns>Raw data object for the report</returns>
    Task<object> GenerateReportDataAsync(CreateReportDTO reportInfo);
}
