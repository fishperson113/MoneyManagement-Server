using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using API.Models.DTOs;
using API.Repositories;
using API.Models.Entities;

namespace API.Hub
{
    public class ChatHub : Hub<IChatClient>
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly MessageRepository _messageRepository;

        public ChatHub(ILogger<ChatHub> logger, MessageRepository messageRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
        }
        // Sends a real-time message to a specific user
        // Also persists the message via repository
        // Notifies both sender and receiver
        public async Task SendMessageToUser(string receiverId, SendMessageDto messageDto)
        {
            try
            {
                // Get the sender ID from the Context
                var senderId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(senderId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Use the repository to save the message
                var message = await _messageRepository.SendMessageAsync(senderId, messageDto);

                // Send to specific user through SignalR
                await Clients.User(receiverId).ReceiveMessage(message);

                // Also send to the sender for consistency
                await Clients.Caller.ReceiveMessage(message);

                _logger.LogInformation("Message sent from {SenderId} to {ReceiverId}", message.SenderId, message.ReceiverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to user {UserId}", receiverId);
                throw;
            }
        }
        // Marks messages as read and notifies the sender
        // Updates read status via repository
        public async Task MarkMessageAsRead(string messageId, string senderId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Mark as read in the repository
                await _messageRepository.MarkMessagesAsReadAsync(userId, senderId);

                // Notify the sender through SignalR
                await Clients.User(senderId).MessageRead(messageId, userId);
                _logger.LogInformation("Message {MessageId} marked as read by {UserId}", messageId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                throw;
            }
        }

        // Adds user to a SignalR group for targeted messaging
        // Useful for multi-device support per user
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} joined their group", userId);
        }
        // Handles new connections, broadcasts online status
        // Triggers when a user connects to SignalR
        public override async Task OnConnectedAsync()
        {
            // Broadcast to all clients that this user is online
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.All.UserOnline(userId);
                _logger.LogInformation("User connected: {UserId}", userId);
            }

            await base.OnConnectedAsync();
        }
        // Handles disconnections, broadcasts offline status
        // Triggers when a user disconnects from SignalR
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Broadcast to all clients that this user is offline
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.All.UserOffline(userId);
                _logger.LogInformation("User disconnected: {UserId}", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
