using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(ITransactionRepository transactionRepository, ILogger<TransactionsController> logger)
        {
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        // GET: api/<TransactionsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetAllTransactions()
        {
            try
            {
                var transactions = await _transactionRepository.GetAllTransactionsAsync();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all transactions");
                return StatusCode(500, "An error occurred while retrieving transactions");
            }
        }

        // GET api/<TransactionsController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDTO>> GetTransactionById(Guid id)
        {
            try
            {
                var transaction = await _transactionRepository.GetTransactionByIdAsync(id);

                if (transaction == null)
                {
                    return NotFound($"Transaction with ID {id} not found");
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving transaction with ID {TransactionId}", id);
                return StatusCode(500, "An error occurred while retrieving the transaction");
            }
        }

        // GET api/<TransactionsController>/wallet/{walletId}
        [HttpGet("wallet/{walletId}")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetTransactionsByWalletId(Guid walletId)
        {
            try
            {
                var transactions = await _transactionRepository.GetTransactionsByWalletIdAsync(walletId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving transactions for wallet with ID {WalletId}", walletId);
                return StatusCode(500, "An error occurred while retrieving transactions");
            }
        }

        // POST api/<TransactionsController>
        [HttpPost]
        public async Task<ActionResult<TransactionDTO>> CreateTransaction([FromBody] CreateTransactionDTO createTransactionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdTransaction = await _transactionRepository.CreateTransactionAsync(createTransactionDTO);
                return CreatedAtAction(nameof(GetTransactionById), new { id = createdTransaction.TransactionID }, createdTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a transaction");
                return StatusCode(500, "An error occurred while creating the transaction");
            }
        }

        // PUT api/<TransactionsController>
        [HttpPut]
        public async Task<ActionResult<TransactionDTO>> UpdateTransaction([FromBody] UpdateTransactionDTO updateTransactionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedTransaction = await _transactionRepository.UpdateTransactionAsync(updateTransactionDTO);
                return Ok(updatedTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating transaction with ID {TransactionId}", updateTransactionDTO.TransactionID);
                return StatusCode(500, "An error occurred while updating the transaction");
            }
        }

        // DELETE api/<TransactionsController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTransaction(Guid id)
        {
            try
            {
                await _transactionRepository.DeleteTransactionByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting transaction with ID {TransactionId}", id);
                return StatusCode(500, "An error occurred while deleting the transaction");
            }
        }

        // GET: api/Transactions/date-range
        // For retrieving transactions by date range with optional filters
        [HttpGet("date-range")]
        public async Task<IActionResult> GetTransactionsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? type = null,
            [FromQuery] string? category = null,
            [FromQuery] string? timeRange = null,
            [FromQuery] string? dayOfWeek = null)
        {
            try
            {
                _logger.LogInformation("Getting transactions by date range from {StartDate} to {EndDate}",
                    startDate, endDate);

                var transactions = await _transactionRepository.GetTransactionsByDateRangeAsync(
                    startDate, endDate, type, category, timeRange, dayOfWeek);

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions by date range");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        // GET: api/Transactions/search
        // For advanced search with multiple filter options
        [HttpGet("search")]
        public async Task<IActionResult> SearchTransactions(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? type = null,
            [FromQuery] string? category = null,
            [FromQuery] string? amountRange = null,
            [FromQuery] string? keywords = null,
            [FromQuery] string? timeRange = null,
            [FromQuery] string? dayOfWeek = null)
        {
            try
            {
                _logger.LogInformation("Searching transactions with complex filters");

                var transactions = await _transactionRepository.SearchTransactionsAsync(
                    startDate, endDate, type, category, amountRange, keywords, timeRange, dayOfWeek);

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching transactions");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
