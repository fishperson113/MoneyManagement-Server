using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;

        public ReportsController(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        // POST: api/Reports/generate
        //[HttpPost("generate")]
        //public async Task<IActionResult> GenerateReport([FromBody] ReportInfoDTO model)
        //{
        //    try
        //    {
        //        var reportInfo = await _transactionRepository.GenerateReportAsync(
        //            model.StartDate, model.EndDate, model.Type, model.Format, model.IncludeTime, model.IncludeDayMonth);

        //        return Ok(reportInfo);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"An error occurred while generating the report: {ex.Message}");
        //    }
        //}

        // GET: api/Reports/{reportId}/download
        //[HttpGet("{reportId}/download")]
        //public async Task<IActionResult> DownloadReport(int reportId)
        //{
        //    try
        //    {
        //        var (fileName, contentType, fileBytes) = await _transactionRepository.DownloadReportAsync(reportId);
        //        return File(fileBytes, contentType, fileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"An error occurred while downloading the report: {ex.Message}");
        //    }
        //}
    }
}
