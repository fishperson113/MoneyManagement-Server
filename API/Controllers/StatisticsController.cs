using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;

        public StatisticsController(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        // GET: api/Statistics/category-breakdown?startDate=2025-01-01&endDate=2025-01-31&type=expense
        [HttpGet("category-breakdown")]
        public async Task<IActionResult> GetCategoryBreakdown(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var breakdown = await _transactionRepository.GetCategoryBreakdownAsync(startDate, endDate);
                return Ok(breakdown);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the category breakdown: {ex.Message}");
            }
        }

        // GET: api/Statistics/cash-flow?startDate=2025-01-01&endDate=2025-01-31
        [HttpGet("cash-flow")]
        public async Task<IActionResult> GetCashFlowSummary(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var summary = await _transactionRepository.GetCashFlowSummaryAsync(startDate, endDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the cash flow summary: {ex.Message}");
            }
        }
    }
}
