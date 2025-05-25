using API.Models.DTOs;
using API.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupFundsController : ControllerBase
    {
        private readonly IGroupFundRepository _repository;
        private readonly ILogger<GroupFundsController> _logger;

        public GroupFundsController(IGroupFundRepository repository, ILogger<GroupFundsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("{groupId}")]
        public async Task<ActionResult<IEnumerable<GroupFundDTO>>> GetGroupFundsByGroupId(GetGroupFundByGroupIdDTO dto)
        {
            try
            {
                var groupFund = await _repository.GetGroupFundsByGroupIdAsync(dto);

                if (groupFund == null)
                {
                    return NotFound($"No group funds found for Group ID {dto.GroupID}");
                }

                return Ok(groupFund);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving group fund with ID {Group ID}", dto.GroupID);
                return StatusCode(500, "An error occurred while retrieving the transaction");
            }
        }

        [HttpPost]
        public async Task<ActionResult<GroupFundDTO>> CreateGroupFund([FromBody] CreateGroupFundDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _repository.CreateGroupFundAsync(dto);
                return CreatedAtAction(nameof(GetGroupFundsByGroupId), new { groupId = result.GroupID }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a group fund");
                return StatusCode(500, "An error occurred while creating the group fund");
            }
        }
    }
}
