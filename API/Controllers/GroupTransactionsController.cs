using API.Models.DTOs;
using API.Repositories;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupTransactionsController : ControllerBase    {
        private readonly GroupTransactionRepository _repository;
        private readonly IGroupFundRepository _groupFundRepository;
        private readonly IGroupFundNotificationService _notificationService;
        private readonly ILogger<GroupTransactionsController> _logger;
        
        public GroupTransactionsController(
            GroupTransactionRepository repository,
            IGroupFundRepository groupFundRepository,
            IGroupFundNotificationService notificationService,
            ILogger<GroupTransactionsController> logger)
        {
            _repository = repository;
            _groupFundRepository = groupFundRepository;
            _notificationService = notificationService;
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
        }        // POST: api/GroupTransactions
        [HttpPost]
        public async Task<ActionResult<GroupTransactionDTO>> Create([FromBody] CreateGroupTransactionDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _repository.CreateGroupTransactionAsync(dto);                // Safe extension: Add group chat message after successful transaction creation
                // This follows the extension pattern by not modifying existing repository logic
                try
                {
                    await NotifyGroupFundUpdateAsync(dto, result);
                }
                catch (Exception notificationEx)
                {
                    // Log notification failure but don't fail the transaction
                    _logger.LogWarning(notificationEx, "Failed to send group transaction message for transaction {TransactionID}", result.GroupTransactionID);
                }

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
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting group transaction");
                return StatusCode(500, "An error occurred while deleting the group transaction.");
            }
        }

        /// <summary>
        /// Private method to handle GroupFund update notifications via SignalR
        /// This is a safe extension that doesn't modify existing repository logic
        /// </summary>
        /// <param name="dto">The original transaction creation request</param>
        /// <param name="result">The created transaction result</param>
        private async Task NotifyGroupFundUpdateAsync(CreateGroupTransactionDTO dto, GroupTransactionDTO result)
        {            try
            {
                // We need to fetch the updated GroupFund information
                // since the repository only returns the transaction data
                var groupFund = await _groupFundRepository.GetGroupFundByIdAsync(dto.GroupFundID);
                  if (groupFund == null)
                {
                    _logger.LogWarning("GroupFund not found for group message: {GroupFundID}", dto.GroupFundID);
                    return;
                }

                var notification = new GroupFundUpdateNotificationDTO
                {
                    GroupFundID = dto.GroupFundID,
                    GroupID = groupFund.GroupID,
                    NewBalance = groupFund.Balance,
                    TotalFundsIn = groupFund.TotalFundsIn,
                    TotalFundsOut = groupFund.TotalFundsOut,
                    TransactionID = result.GroupTransactionID,
                    TransactionType = dto.Type,
                    TransactionAmount = dto.Amount,
                    TransactionDescription = dto.Description,
                    UpdatedAt = DateTime.UtcNow,
                    UserId = GetCurrentUserId()
                };

                await _notificationService.SendGroupTransactionMessageAsync(notification);
                
                _logger.LogInformation("Successfully sent group transaction message for transaction {TransactionID}", result.GroupTransactionID);
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group transaction message for transaction {TransactionID}", result.GroupTransactionID);
                throw;
            }
        }

        /// <summary>
        /// Helper method to get the current user ID from HTTP context
        /// </summary>
        private string GetCurrentUserId()
        {
            return HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User is not authenticated");
        }
    }
}
