using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using API.Models.DTOs;
using API.Repositories;
using API.Models.Entities;
using System.Collections.Concurrent;

namespace API.Hub
{
    public class ChatHub : Hub<IChatClient>
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly MessageRepository _messageRepository;
        private readonly FriendRepository _friendRepository;

        // Track online users and their connection IDs
        private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = new();

        public ChatHub(ILogger<ChatHub> logger, MessageRepository messageRepository, FriendRepository friendRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
            _friendRepository = friendRepository;
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

                // Check if receiver is online
                var isReceiverOnline = OnlineUsers.ContainsKey(receiverId);

                // Send to specific user through SignalR if they're online
                if (isReceiverOnline)
                {
                    await Clients.User(receiverId).ReceiveMessage(message);
                }
                else
                {
                    // If receiver is offline, they will get the message when they come back online
                    // The message is already saved in the database
                    _logger.LogInformation("User {ReceiverId} is offline. Message will be delivered when they connect.", receiverId);
                }

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
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to online users dictionary
                if (!OnlineUsers.TryGetValue(userId, out var connections))
                {
                    connections = new HashSet<string>();
                    OnlineUsers[userId] = connections;
                }
                connections.Add(Context.ConnectionId);

                // Broadcast online status to friends only
                var friends = await _friendRepository.GetUserFriendsAsync(userId);
                foreach (var friend in friends)
                {
                    await Clients.User(friend.UserId).UserOnline(userId);
                }

                _logger.LogInformation("User connected: {UserId}", userId);

                // Check for unread messages when user comes online
                // This is where we handle "offline messaging" delivery
                var unreadChats = await _messageRepository.GetUserChatsAsync(userId);
                foreach (var chat in unreadChats)
                {
                    // Notify the client about chats with unread messages
                    if (chat.Messages.Any(m => m.ReceiverId == userId))
                    {
                        await Clients.User(userId).NewUnreadMessages(chat.OtherUserId);
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        // Handles disconnections, broadcasts offline status
        // Triggers when a user disconnects from SignalR
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Remove connection ID from online users
                if (OnlineUsers.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);

                    // If no more connections for this user, remove from online users
                    if (connections.Count == 0)
                    {
                        OnlineUsers.TryRemove(userId, out _);

                        // Broadcast offline status to friends only
                        var friends = await _friendRepository.GetUserFriendsAsync(userId);
                        foreach (var friend in friends)
                        {
                            await Clients.User(friend.UserId).UserOffline(userId);
                        }
                    }
                }

                _logger.LogInformation("User disconnected: {UserId}", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Check if a specific user is online
        public async Task<bool> IsUserOnline(string userId)
        {
            return OnlineUsers.ContainsKey(userId);
        }

        // Get all online friends for the current user
        public async Task<List<string>> GetOnlineFriends()
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User is not authenticated");
            }

            var friends = await _friendRepository.GetUserFriendsAsync(userId);
            return friends.Where(f => OnlineUsers.ContainsKey(f.UserId)).Select(f => f.UserId).ToList();
        }

        // Accepts a friend request and notifies both users
        public async Task AcceptFriendRequest(string friendId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Accept friend request via repository
                var success = await _friendRepository.AcceptFriendRequestAsync(userId, friendId);

                if (success)
                {
                    // Notify the original requester (friend) that their request was accepted
                    await Clients.User(friendId).FriendRequestAccepted(userId);

                    // Also notify the current user (for UI consistency across devices)
                    await Clients.Caller.FriendRequestAccepted(friendId);

                    _logger.LogInformation("Friend request accepted: {UserId} accepted {FriendId}'s request", userId, friendId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request");
                throw;
            }
        }

    }

}
