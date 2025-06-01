using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupTransactionsController : ControllerBase
    {
        private readonly GroupTransactionRepository _repository;
        private readonly ILogger<GroupTransactionsController> _logger;
        public GroupTransactionsController(GroupTransactionRepository repository, ILogger<GroupTransactionsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: api/GroupTransactions/{groupFundId}
        [HttpGet("{groupFundId}")]
        public async Task<ActionResult<IEnumerable<GroupTransactionDTO>>> GetByGroupFundId(Guid groupFundId)
        {
            try
            {
                var transactions = await _repository.GetGroupTransactionsByFundIdAsync(groupFundId);

                if (transactions == null || !transactions.Any())
                    return NotFound($"No transactions found for GroupFundID {groupFundId}");

                return Ok(transactions);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Unauthorized access to group fund {GroupFundID}", groupFundId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching transactions for GroupFundID {GroupFundID}", groupFundId);
                return StatusCode(500, "An error occurred while retrieving group transactions.");
            }
        }

        // POST: api/GroupTransactions
        [HttpPost]
        public async Task<ActionResult<GroupTransactionDTO>> Create([FromBody] CreateGroupTransactionDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _repository.CreateGroupTransactionAsync(dto);

                return CreatedAtAction(nameof(GetByGroupFundId), new { groupFundId = result.GroupFundID }, result);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Unauthorized create attempt for GroupFundID {GroupFundID}", dto.GroupFundID);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating group transaction");
                return StatusCode(500, "An error occurred while creating the group transaction.");
            }
        }

        [HttpPut]
        public async Task<ActionResult<GroupTransactionDTO>> Update([FromBody] UpdateGroupTransactionDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _repository.UpdateGroupTransactionAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating group transaction");
                return StatusCode(500, "An error occurred while updating the group transaction.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var success = await _repository.DeleteGroupTransactionAsync(id);
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting group transaction");
                return StatusCode(500, "An error occurred while deleting the group transaction.");
            }
        }
    }
}
