using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupTransactionCommentController : ControllerBase
    {
        private readonly GroupTransactionCommentRepository _repository;

        public GroupTransactionCommentController(GroupTransactionCommentRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Get all comments for a group transaction
        /// </summary>
        [HttpGet("transaction/{transactionId}")]
        [SwaggerOperation(
            Summary = "Get all comments for a group transaction",
            Description = "Retrieves all comments associated with the specified group transaction"
        )]
        [SwaggerResponse(200, "List of comments retrieved successfully", typeof(IEnumerable<GroupTransactionCommentDTO>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Transaction not found")]
        public async Task<ActionResult<IEnumerable<GroupTransactionCommentDTO>>> GetCommentsByTransactionId(Guid transactionId)
        {
            try
            {
                var comments = await _repository.GetCommentsByTransactionIdAsync(transactionId);
                return Ok(comments);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a new comment to a group transaction
        /// </summary>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Add a new comment to a group transaction",
            Description = "Creates a new comment on the specified group transaction"
        )]
        [SwaggerResponse(201, "Comment created successfully", typeof(GroupTransactionCommentDTO))]
        [SwaggerResponse(400, "Invalid input")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Transaction not found")]
        public async Task<ActionResult<GroupTransactionCommentDTO>> AddComment(CreateGroupTransactionCommentDTO dto)
        {
            try
            {
                var comment = await _repository.AddCommentAsync(dto);
                return CreatedAtAction(nameof(GetCommentsByTransactionId),
                    new { transactionId = dto.GroupTransactionId }, comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing comment
        /// </summary>
        [HttpPut]
        [SwaggerOperation(
            Summary = "Update an existing comment",
            Description = "Updates the content of an existing comment"
        )]
        [SwaggerResponse(200, "Comment updated successfully", typeof(GroupTransactionCommentDTO))]
        [SwaggerResponse(400, "Invalid input")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden - not the comment author")]
        [SwaggerResponse(404, "Comment not found")]
        public async Task<ActionResult<GroupTransactionCommentDTO>> UpdateComment(UpdateGroupTransactionCommentDTO dto)
        {
            try
            {
                var comment = await _repository.UpdateCommentAsync(dto);
                return Ok(comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        [HttpDelete("{commentId}")]
        [SwaggerOperation(
            Summary = "Delete a comment",
            Description = "Deletes an existing comment"
        )]
        [SwaggerResponse(204, "Comment deleted successfully")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden - not the comment author")]
        [SwaggerResponse(404, "Comment not found")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            try
            {
                await _repository.DeleteCommentAsync(commentId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}