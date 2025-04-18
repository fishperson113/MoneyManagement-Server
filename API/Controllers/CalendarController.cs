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

        // GET: api/Calendar/daily?date=2025-01-01
        [HttpGet("daily")]
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

        // GET: api/Calendar/weekly?startDate=2025-01-01
        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklySummary([FromQuery] DateTime startDate)
        {
            try
            {
                // Find the previous Monday if startDate is not explicitly provided
                if (!Request.Query.ContainsKey("startDate"))
                {
                    var today = DateTime.UtcNow.Date;
                    var daysUntilPreviousMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                    startDate = today.AddDays(-daysUntilPreviousMonday);
                }

                var summary = await _transactionRepository.GetWeeklySummaryAsync(startDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the weekly summary: {ex.Message}");
            }
        }

        // GET: api/Calendar/monthly?year=2025&month=1
        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthlySummary([FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                // Default to current year and month if not provided
                year ??= DateTime.UtcNow.Year;
                month ??= DateTime.UtcNow.Month;

                var monthDate = new DateTime(year.Value, month.Value, 1);
                var summary = await _transactionRepository.GetMonthlySummaryAsync(monthDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the monthly summary: {ex.Message}");
            }
        }

        // GET: api/Calendar/yearly?year=2025
        [HttpGet("yearly")]
        public async Task<IActionResult> GetYearlySummary([FromQuery] int? year)
        {
            try
            {
                // Default to current year if not provided
                year ??= DateTime.UtcNow.Year;

                var summary = await _transactionRepository.GetYearlySummaryAsync(year.Value);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the yearly summary: {ex.Message}");
            }
        }

        // GET: api/Calendar/summary?date=2025-01-01 (keeping for backward compatibility)
        [HttpGet("summary")]
        public async Task<IActionResult> GetDailySummaryLegacy([FromQuery] DateTime date)
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
