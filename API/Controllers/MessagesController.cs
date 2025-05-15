using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Data;
using API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using API.Models.DTOs;
using API.Repositories;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly MessageRepository _messageRepository;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(MessageRepository messageRepository, ILogger<MessagesController> logger)
        {
            _messageRepository = messageRepository;
            _logger = logger;
        }
        /// <summary>
        /// Sends a new message to another user
        /// </summary>
        /// <param name="dto">The message data containing recipient ID and content</param>
        /// <returns>The created message with full sender and receiver details</returns>
        /// <response code="200">Returns the created message</response>
        /// <response code="400">If the request is invalid or trying to message yourself</response>
        /// <response code="500">If there was an internal server error</response>

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            try
            {
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (senderId == null || senderId == dto.ReceiverId)
                    return BadRequest("Không hợp lệ hoặc gửi cho chính mình.");

                var message = await _messageRepository.SendMessageAsync(senderId, dto);
                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, "Có lỗi xảy ra khi gửi tin nhắn.");
            }
        }
        /// <summary>
        /// Gets all messages between current user and specified user
        /// For paginating older messages or refreshing specific messages
        /// </summary>
        /// <param name="receiverId">The ID of the other user in the conversation</param>
        /// <returns>Chronological list of messages between the two users</returns>
        /// <response code="200">Returns the list of messages</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("{receiverId}")]
        public async Task<IActionResult> GetMessages(string receiverId)
        {
            try
            {
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (senderId == null)
                    return Unauthorized();

                var chatHistory = await _messageRepository.GetChatHistoryAsync(senderId, receiverId);
                return Ok(chatHistory.Messages);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Không tìm thấy cuộc trò chuyện hoặc người dùng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages");
                return StatusCode(500, "Có lỗi xảy ra khi lấy tin nhắn.");
            }
        }

        /// <summary>
        /// Gets all chat conversations for the current user
        /// </summary>
        /// <returns>List of chats with other users, including recent messages</returns>
        /// <response code="200">Returns the list of chat conversations</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If there was an internal server error</response>

        [HttpGet("chats")]
        public async Task<IActionResult> GetUserChats()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var chats = await _messageRepository.GetUserChatsAsync(userId);
                return Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user chats");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách các cuộc trò chuyện.");
            }
        }

        /// <summary>
        /// Gets detailed chat history with specific user
        /// For initial chat load with metadata
        /// </summary>
        /// <param name="otherUserId">The ID of the other user in the conversation</param>
        /// <returns>Chat metadata and complete message history</returns>
        /// <response code="200">Returns the chat history</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the conversation or user was not found</response>
        /// <response code="500">If there was an internal server error</response>

        [HttpGet("chat/{otherUserId}")]
        public async Task<IActionResult> GetChatHistory(string otherUserId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var chatHistory = await _messageRepository.GetChatHistoryAsync(userId, otherUserId);
                return Ok(chatHistory);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Không tìm thấy cuộc trò chuyện hoặc người dùng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history");
                return StatusCode(500, "Có lỗi xảy ra khi lấy lịch sử trò chuyện.");
            }
        }
        /// <summary>
        /// Marks all messages from specified user as read
        /// </summary>
        /// <param name="otherUserId">The ID of the user whose messages to mark as read</param>
        /// <returns>Success status of the operation</returns>
        /// <response code="200">If messages were successfully marked as read</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If there was an internal server error</response>

        [HttpPost("read/{otherUserId}")]
        public async Task<IActionResult> MarkMessagesAsRead(string otherUserId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var result = await _messageRepository.MarkMessagesAsReadAsync(userId, otherUserId);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read");
                return StatusCode(500, "Có lỗi xảy ra khi đánh dấu tin nhắn đã đọc.");
            }
        }
        /// <summary>
        /// Deletes a specific message
        /// </summary>
        /// <param name="messageId">The ID of the message to delete</param>
        /// <returns>Success status of the operation</returns>
        /// <remarks>
        /// User must be either the sender or receiver of the message to delete it.
        /// Message will be removed from both SQL database and Firestore.
        /// </remarks>
        /// <response code="200">If the message was successfully deleted</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the message was not found or user lacks permission</response>
        /// <response code="500">If there was an internal server error</response>

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var result = await _messageRepository.DeleteMessageAsync(userId, messageId);
                if (!result)
                    return NotFound("Không tìm thấy tin nhắn hoặc bạn không có quyền xóa tin nhắn này.");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return StatusCode(500, "Có lỗi xảy ra khi xóa tin nhắn.");
            }
        }
        /// <summary>
        /// Gets the newest message from each conversation for the current user
        /// </summary>
        /// <returns>Dictionary of user IDs and their most recent messages</returns>
        /// <response code="200">Returns the map of latest messages</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("latest")]
        public async Task<IActionResult> GetAllLatestMessages()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var latestMessages = await _messageRepository.GetAllLatestMessagesAsync(userId);
                return Ok(latestMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest messages");
                return StatusCode(500, "Có lỗi xảy ra khi lấy tin nhắn mới nhất.");
            }
        }


    }
}
