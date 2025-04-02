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
    }
}
