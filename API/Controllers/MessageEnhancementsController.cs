using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller for message enhancement features (reactions, mentions)
/// Safe extension: New controller that doesn't modify existing message controllers
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MessageEnhancementsController : ControllerBase
{
    private readonly IMessageEnhancementRepository _repository;
    private readonly ILogger<MessageEnhancementsController> _logger;

    public MessageEnhancementsController(
        IMessageEnhancementRepository repository,
        ILogger<MessageEnhancementsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    #region Message Reactions

    /// <summary>
    /// Adds a reaction to a message
    /// </summary>
    /// <param name="dto">Reaction details</param>
    /// <returns>Created reaction</returns>
    [HttpPost("reactions")]
    public async Task<ActionResult<MessageReactionDTO>> AddReaction([FromBody] CreateMessageReactionDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reaction = await _repository.AddReactionAsync(dto);
            return Ok(reaction);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized reaction attempt");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction to message {MessageId}", dto.MessageId);
            return StatusCode(500, "An error occurred while adding the reaction.");
        }
    }

    /// <summary>
    /// Removes a reaction from a message
    /// </summary>
    /// <param name="dto">Reaction removal details</param>
    /// <returns>Success status</returns>
    [HttpDelete("reactions")]
    public async Task<ActionResult> RemoveReaction([FromBody] RemoveMessageReactionDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _repository.RemoveReactionAsync(dto);
            if (!success)
                return NotFound("Reaction not found");

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized reaction removal attempt");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from message {MessageId}", dto.MessageId);
            return StatusCode(500, "An error occurred while removing the reaction.");
        }
    }

    /// <summary>
    /// Gets all reactions for a message
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="messageType">Message type (direct/group)</param>
    /// <returns>Reaction summary</returns>
    [HttpGet("reactions/{messageId}")]
    public async Task<ActionResult<MessageReactionSummaryDTO>> GetMessageReactions(
        Guid messageId, 
        [FromQuery] string messageType = "direct")
    {
        try
        {
            var reactions = await _repository.GetMessageReactionsAsync(messageId, messageType);
            return Ok(reactions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to message reactions");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reactions for message {MessageId}", messageId);
            return StatusCode(500, "An error occurred while retrieving reactions.");
        }
    }

    /// <summary>
    /// Gets reactions for multiple messages (for chat history)
    /// </summary>
    /// <param name="request">Message IDs and type</param>
    /// <returns>Dictionary of message reactions</returns>
    [HttpPost("reactions/batch")]
    public async Task<ActionResult<Dictionary<Guid, MessageReactionSummaryDTO>>> GetMultipleMessageReactions(
        [FromBody] BatchMessageReactionsRequestDTO request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reactions = await _repository.GetMultipleMessageReactionsAsync(request.MessageIds, request.MessageType);
            return Ok(reactions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to batch message reactions");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch message reactions");
            return StatusCode(500, "An error occurred while retrieving reactions.");
        }
    }

    #endregion

    #region Message Mentions

    /// <summary>
    /// Gets all mentions for a message
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <returns>List of mentions</returns>
    [HttpGet("mentions/{messageId}")]
    public async Task<ActionResult<List<MessageMentionDTO>>> GetMessageMentions(Guid messageId)
    {
        try
        {
            var mentions = await _repository.GetMessageMentionsAsync(messageId);
            return Ok(mentions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to message mentions");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentions for message {MessageId}", messageId);
            return StatusCode(500, "An error occurred while retrieving mentions.");
        }
    }

    /// <summary>
    /// Gets all unread mentions for the current user
    /// </summary>
    /// <returns>List of unread mention notifications</returns>
    [HttpGet("mentions/unread")]
    public async Task<ActionResult<List<MentionNotificationDTO>>> GetUnreadMentions()
    {
        try
        {
            var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var mentions = await _repository.GetUnreadMentionsAsync(userId);
            return Ok(mentions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread mentions");
            return StatusCode(500, "An error occurred while retrieving unread mentions.");
        }
    }

    /// <summary>
    /// Marks a mention as read
    /// </summary>
    /// <param name="mentionId">Mention ID</param>
    /// <returns>Success status</returns>
    [HttpPut("mentions/{mentionId}/read")]
    public async Task<ActionResult> MarkMentionAsRead(Guid mentionId)
    {
        try
        {
            var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var success = await _repository.MarkMentionAsReadAsync(mentionId, userId);
            if (!success)
                return NotFound("Mention not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking mention {MentionId} as read", mentionId);
            return StatusCode(500, "An error occurred while marking mention as read.");
        }
    }

    /// <summary>
    /// Marks all mentions as read for the current user
    /// </summary>
    /// <returns>Number of mentions marked as read</returns>
    [HttpPut("mentions/read-all")]
    public async Task<ActionResult<int>> MarkAllMentionsAsRead()
    {
        try
        {
            var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var count = await _repository.MarkAllMentionsAsReadAsync(userId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all mentions as read");
            return StatusCode(500, "An error occurred while marking mentions as read.");
        }
    }

    #endregion

    #region Enhanced Messages

    /// <summary>
    /// Gets an enhanced message with reactions and mentions
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="messageType">Message type (direct/group)</param>
    /// <returns>Enhanced message with reactions and mentions</returns>
    [HttpGet("enhanced/{messageId}")]
    public async Task<ActionResult<EnhancedMessageDTO>> GetEnhancedMessage(
        Guid messageId, 
        [FromQuery] string messageType = "direct")
    {
        try
        {
            var enhancedMessage = await _repository.GetEnhancedMessageAsync(messageId, messageType);
            if (enhancedMessage == null)
                return NotFound("Message not found");

            return Ok(enhancedMessage);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to enhanced message");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced message {MessageId}", messageId);
            return StatusCode(500, "An error occurred while retrieving the enhanced message.");
        }
    }

    /// <summary>
    /// Gets enhanced messages for multiple message IDs (for chat history)
    /// </summary>
    /// <param name="request">Message IDs and type</param>
    /// <returns>List of enhanced messages</returns>
    [HttpPost("enhanced/batch")]
    public async Task<ActionResult<List<EnhancedMessageDTO>>> GetEnhancedMessages(
        [FromBody] BatchMessageReactionsRequestDTO request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var enhancedMessages = await _repository.GetEnhancedMessagesAsync(request.MessageIds, request.MessageType);
            return Ok(enhancedMessages);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to enhanced messages");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced messages");
            return StatusCode(500, "An error occurred while retrieving enhanced messages.");
        }
    }

    #endregion
}

/// <summary>
/// DTO for batch message reactions request
/// </summary>
public class BatchMessageReactionsRequestDTO
{
    public List<Guid> MessageIds { get; set; } = new();
    public string MessageType { get; set; } = "direct";
}
