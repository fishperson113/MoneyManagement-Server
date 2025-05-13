using API.Controllers;
using API.Data;
using AutoMapper;
using Google.Cloud.Firestore;
using System.Security.Claims;
using API.Models.Entities;
using API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
namespace API.Repositories
{
    public class MessageRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageRepository> _logger;
        private readonly FirestoreDb _firestoreDb;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MessageRepository(
            ApplicationDbContext dbContext,
            IMapper mapper,
            ILogger<MessageRepository> logger,
            FirestoreDb firestoreDb,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _firestoreDb = firestoreDb;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User is not authenticated");
        }

        public async Task<MessageDTO> SendMessageAsync(string senderId, SendMessageDto dto)
        {
            try
            {
                _logger.LogInformation("Sending message from {SenderId} to {ReceiverId}", senderId, dto.ReceiverId);

                if (senderId == dto.ReceiverId)
                {
                    throw new InvalidOperationException("Cannot send message to yourself");
                }

                // Lưu vào SQL
                var message = new Message
                {
                    MessageID = Guid.NewGuid(),
                    SenderId = senderId,
                    ReceiverId = dto.ReceiverId,
                    Content = dto.Content,
                    SentAt = DateTime.UtcNow
                };
                _dbContext.Messages.Add(message);
                await _dbContext.SaveChangesAsync();

                // Lưu vào Firestore
                var chatId = string.CompareOrdinal(senderId, dto.ReceiverId) < 0
                    ? $"{senderId}_{dto.ReceiverId}"
                    : $"{dto.ReceiverId}_{senderId}";

                var docRef = _firestoreDb.Collection("privateMessages")
                    .Document(chatId)
                    .Collection("messages")
                    .Document(message.MessageID.ToString());

                await docRef.SetAsync(new
                {
                    messageId = message.MessageID.ToString(),
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    content = message.Content,
                    sentAt = message.SentAt,
                    isRead = false
                });

                // Load sender and receiver data
                await _dbContext.Entry(message).Reference(m => m.Sender).LoadAsync();
                await _dbContext.Entry(message).Reference(m => m.Receiver).LoadAsync();

                var messageDto = _mapper.Map<MessageDTO>(message);
                messageDto.SenderName = $"{message.Sender.FirstName} {message.Sender.LastName}";
                messageDto.ReceiverName = $"{message.Receiver.FirstName} {message.Receiver.LastName}";

                return messageDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from {SenderId} to {ReceiverId}", senderId, dto.ReceiverId);
                throw;
            }
        }

        public async Task<IEnumerable<MessageDTO>> GetMessagesBetweenUsersAsync(string currentUserId, string otherUserId)
        {
            try
            {
                _logger.LogInformation("Getting messages between {CurrentUserId} and {OtherUserId}", currentUserId, otherUserId);

                var messages = await _dbContext.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                var messagesList = messages.Select(m => new MessageDTO
                {
                    MessageID = m.MessageID,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                    ReceiverName = $"{m.Receiver.FirstName} {m.Receiver.LastName}"
                }).ToList();

                return messagesList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages between users");
                throw;
            }
        }

        public async Task<IEnumerable<ChatHistoryDTO>> GetUserChatsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting all chats for user {UserId}", userId);

                // Get all users that the current user has exchanged messages with
                var chatPartners = await _dbContext.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToListAsync();

                var result = new List<ChatHistoryDTO>();

                foreach (var partnerId in chatPartners)
                {
                    // Get partner user data
                    var partner = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.Id == partnerId);

                    if (partner == null) continue;

                    // Get last X messages with this partner
                    var messages = await _dbContext.Messages
                        .Where(m => (m.SenderId == userId && m.ReceiverId == partnerId) ||
                                    (m.SenderId == partnerId && m.ReceiverId == userId))
                        .OrderByDescending(m => m.SentAt)
                        .Take(20)  // Get last 20 messages
                        .Include(m => m.Sender)
                        .Include(m => m.Receiver)
                        .ToListAsync();

                    var chatId = string.CompareOrdinal(userId, partnerId) < 0
                        ? $"{userId}_{partnerId}"
                        : $"{partnerId}_{userId}";

                    result.Add(new ChatHistoryDTO
                    {
                        ChatId = chatId,
                        Messages = messages.Select(m => new MessageDTO
                        {
                            MessageID = m.MessageID,
                            SenderId = m.SenderId,
                            ReceiverId = m.ReceiverId,
                            Content = m.Content,
                            SentAt = m.SentAt,
                            SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                            ReceiverName = $"{m.Receiver.FirstName} {m.Receiver.LastName}"
                        }).OrderBy(m => m.SentAt).ToList()
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user chats");
                throw;
            }
        }

        public async Task<bool> MarkMessagesAsReadAsync(string userId, string otherUserId)
        {
            try
            {
                _logger.LogInformation("Marking messages as read from {OtherUserId} to {UserId}", otherUserId, userId);

                // Create chat ID
                var chatId = string.CompareOrdinal(userId, otherUserId) < 0
                    ? $"{userId}_{otherUserId}"
                    : $"{otherUserId}_{userId}";

                // Update Firebase messages to mark as read
                var messagesRef = _firestoreDb.Collection("privateMessages")
                    .Document(chatId)
                    .Collection("messages");

                var query = messagesRef.WhereEqualTo("receiverId", userId).WhereEqualTo("isRead", false);
                var snapshot = await query.GetSnapshotAsync();

                var batch = _firestoreDb.StartBatch();
                foreach (var document in snapshot.Documents)
                {
                    batch.Update(document.Reference, new Dictionary<string, object> { { "isRead", true } });
                }

                await batch.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read");
                return false;
            }
        }

        public async Task<ChatHistoryDTO> GetChatHistoryAsync(string userId, string otherUserId)
        {
            try
            {
                _logger.LogInformation("Getting chat history between {UserId} and {OtherUserId}", userId, otherUserId);

                // Get partner user data
                var partner = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == otherUserId);

                if (partner == null)
                {
                    throw new KeyNotFoundException($"User with ID {otherUserId} not found");
                }

                // Get messages between the users
                var messages = await GetMessagesBetweenUsersAsync(userId, otherUserId);

                var chatId = string.CompareOrdinal(userId, otherUserId) < 0
                    ? $"{userId}_{otherUserId}"
                    : $"{otherUserId}_{userId}";

                return new ChatHistoryDTO
                {
                    ChatId = chatId,
                    Messages = messages.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat history");
                throw;
            }
        }

        public async Task<bool> DeleteMessageAsync(string userId, Guid messageId)
        {
            try
            {
                _logger.LogInformation("Deleting message {MessageId} by user {UserId}", messageId, userId);

                var message = await _dbContext.Messages
                    .FirstOrDefaultAsync(m => m.MessageID == messageId &&
                                          (m.SenderId == userId || m.ReceiverId == userId));

                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found or user {UserId} doesn't have permission", messageId, userId);
                    return false;
                }

                // Delete from SQL
                _dbContext.Messages.Remove(message);
                await _dbContext.SaveChangesAsync();

                // Delete from Firestore
                var chatId = string.CompareOrdinal(message.SenderId, message.ReceiverId) < 0
                    ? $"{message.SenderId}_{message.ReceiverId}"
                    : $"{message.ReceiverId}_{message.SenderId}";

                var docRef = _firestoreDb.Collection("privateMessages")
                    .Document(chatId)
                    .Collection("messages")
                    .Document(message.MessageID.ToString());

                await docRef.DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return false;
            }
        }
    }
}
