using API.Models.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller for report generation operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }    /// <summary>
    /// Generates a financial report and returns it as a downloadable file
    /// </summary>
    /// <param name="model">Report configuration including date range, type, format, and currency</param>
    /// <returns>Downloadable report file</returns>
    /// <example>
    /// POST /api/Reports/generate
    /// {
    ///   "startDate": "2025-01-01",
    ///   "endDate": "2025-01-31", 
    ///   "type": "cash-flow",
    ///   "format": "pdf",
    ///   "currency": "USD"
    /// }
    /// </example>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateReport([FromBody] CreateReportDTO model)
    {
        try
        {
            _logger.LogInformation("Received report generation request for type {Type} from {Start} to {End}",
                model.Type, model.StartDate, model.EndDate);

            var reportInfo = await _reportService.GenerateReportAsync(model);
            
            // Read the generated file and return it as downloadable content
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            var fileName = Path.GetFileName(reportInfo.DownloadUrl.TrimStart('/').Replace("Reports/", ""));
            var filePath = Path.Combine(reportsDirectory, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Generated report file not found: {FilePath}", filePath);
                return StatusCode(500, new { error = "Generated report file not found" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(fileName);

            _logger.LogInformation("Successfully generated and serving report file: {FileName}", fileName);

            // Clean up the file after reading (optional - you might want to keep files for caching)
            try
            {
                System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary report file: {FilePath}", filePath);
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters for report generation");
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for report generation");
            return Unauthorized(new { error = "Access denied" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while generating report");
            return StatusCode(500, new { error = "An internal server error occurred while generating the report" });
        }
    }

    /// <summary>
    /// Gets the appropriate content type for a file
    /// </summary>
    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
}
