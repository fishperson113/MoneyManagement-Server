using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;

        public CalendarController(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        // GET: api/Calendar/summary?date=2025-01-01
        [HttpGet("summary")]
        public async Task<IActionResult> GetDailySummary([FromQuery] DateTime date)
        {
            try
            {
                var summary = await _transactionRepository.GetDailySummaryAsync(date);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the daily summary: {ex.Message}");
            }
        }
    }
}
