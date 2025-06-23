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
        private readonly GroupRepository _groupRepository;
        private readonly IMessageEnhancementRepository _messageEnhancementRepository;

        // Track online users and their connection IDs
        private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = new();

        public ChatHub(ILogger<ChatHub> logger, MessageRepository messageRepository,
            FriendRepository friendRepository, GroupRepository groupRepository,
            IMessageEnhancementRepository messageEnhancementRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
            _friendRepository = friendRepository;
            _groupRepository = groupRepository;
            _messageEnhancementRepository = messageEnhancementRepository;
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
                // Parse and process mentions in the message
                var mentionedUserIds = await ParseMentionsFromContent(messageDto.Content);
                if (mentionedUserIds.Any())
                {
                    // Only allow mentions of the receiver in direct messages
                    var validMentions = mentionedUserIds.Where(id => id == receiverId).ToList();
                    if (validMentions.Any())
                    {
                        await NotifyMentions(message.MessageID, messageDto.Content, "direct");
                    }
                }

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
        
        /// <summary>
        /// Sends a message to all members of a group
        /// </summary>
        /// <param name="messageDto">The message data containing group ID and content</param>
        public async Task SendMessageToGroup(SendGroupMessageDTO messageDto)
        {
            try
            {
                // Get the sender ID from the Context
                var senderId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(senderId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Use repository to save the message
                var message = await _groupRepository.SendGroupMessageAsync(senderId, messageDto);                
                // Parse and process mentions in the group message
                var mentionedUserIds = await ParseMentionsFromContent(messageDto.Content);
                if (mentionedUserIds.Any())
                {
                    // Get group members to validate mentions
                    var groupMembers = await _groupRepository.GetGroupMembersAsync(senderId, messageDto.GroupId);
                    var groupMemberIds = groupMembers.Select(m => m.UserId).ToHashSet();
                    
                    // Only allow mentions of group members (excluding the sender)
                    var validMentions = mentionedUserIds
                        .Where(id => groupMemberIds.Contains(id) && id != senderId)
                        .ToList();
                    
                    if (validMentions.Any())
                    {
                        await NotifyMentions(message.MessageId, messageDto.Content, "group", messageDto.GroupId);
                    }
                }

                // Get all members of the group for message distribution
                //var allGroupMembers = await _groupRepository.GetGroupMembersAsync(senderId, messageDto.GroupId);

                //// Send to all group members (including sender for consistency)
                //foreach (var member in allGroupMembers)
                //{
                //    if (OnlineUsers.ContainsKey(member.UserId))
                //    {
                //        await Clients.User(member.UserId).ReceiveGroupMessage(message);
                //    }
                //}
                await Clients.Group($"group_{messageDto.GroupId}").ReceiveGroupMessage(message);
                await Clients.Caller.ReceiveGroupMessage(message);

                _logger.LogInformation("Group message sent from {SenderId} to group {GroupId}",
                    senderId, messageDto.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to group {GroupId}", messageDto.GroupId);
                throw;
            }
        }

        /// <summary>
        /// Marks all messages in a group as read for the current user
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        public async Task MarkGroupMessagesAsRead(Guid groupId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Mark as read in the repository
                await _groupRepository.MarkGroupMessagesAsReadAsync(userId, groupId);

                // Notify other group members that this user has read messages
                await Clients.Group($"group_{groupId}").GroupMessagesRead(groupId, userId);

                _logger.LogInformation("Messages in group {GroupId} marked as read by {UserId}", groupId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking group messages as read");
                throw;
            }
        }

        /// <summary>
        /// Adds a user to a SignalR group for a chat group
        /// </summary>
        /// <param name="groupId">The ID of the group to join</param>
        public async Task JoinGroupChat(Guid groupId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Add connection to the SignalR group (different from the chat group entity)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");

                _logger.LogInformation("User {UserId} joined SignalR group for group chat {GroupId}",
                    userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining group chat");
                throw;
            }
        }

        /// <summary>
        /// Notifies users when someone is added to a group
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <param name="userId">The ID of the user who was added</param>
        public async Task NotifyUserAddedToGroup(Guid groupId, string userId)
        {
            try
            {
                var currentUserId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Get the group info
                var group = await _groupRepository.GetUserGroupsAsync(userId);
                var groupDto = group.FirstOrDefault(g => g.GroupId == groupId);

                if (groupDto != null)
                {
                    // Notify the added user
                    if (OnlineUsers.ContainsKey(userId))
                    {
                        await Clients.User(userId).UserAddedToGroup(groupDto);
                    }

                    // Notify existing group members
                    await Clients.Group($"group_{groupId}").UserAddedToGroup(groupDto);
                }

                _logger.LogInformation("User {UserId} was added to group {GroupId} by {CurrentUserId}",
                    userId, groupId, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying group about new member");
                throw;
            }
        }

        /// <summary>
        /// Notifies users when someone is removed from a group
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <param name="userId">The ID of the user who was removed</param>
        public async Task NotifyUserRemovedFromGroup(Guid groupId, string userId)
        {
            try
            {
                var currentUserId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Notify the removed user
                if (OnlineUsers.ContainsKey(userId))
                {
                    await Clients.User(userId).UserRemovedFromGroup(groupId, userId);
                }

                // Notify remaining group members
                await Clients.Group($"group_{groupId}").UserRemovedFromGroup(groupId, userId);

                _logger.LogInformation("User {UserId} was removed from group {GroupId} by {CurrentUserId}",
                    userId, groupId, currentUserId);

                // Remove the user from the SignalR group
                var userConnections = OnlineUsers.GetValueOrDefault(userId);
                if (userConnections != null)
                {
                    foreach (var connectionId in userConnections)
                    {
                        await Groups.RemoveFromGroupAsync(connectionId, $"group_{groupId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying about removed group member");
                throw;
            }
        }

        // Add group handling to OnConnectedAsync
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Existing code for tracking online users
                if (!OnlineUsers.TryGetValue(userId, out var connections))
                {
                    connections = new HashSet<string>();
                    OnlineUsers[userId] = connections;
                }
                connections.Add(Context.ConnectionId);

                // Get and join all the user's groups
                var userGroups = await _groupRepository.GetUserGroupsAsync(userId);
                foreach (var group in userGroups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{group.GroupId}");
                    _logger.LogInformation("User {UserId} auto-joined SignalR group for chat group {GroupId}",
                        userId, group.GroupId);
                }

                // Broadcast online status to friends only (existing code)
                var friends = await _friendRepository.GetUserFriendsAsync(userId);
                foreach (var friend in friends)
                {
                    await Clients.User(friend.UserId).UserOnline(userId);
                }

                _logger.LogInformation("User connected: {UserId}", userId);

                // Existing code for checking unread private messages
                var unreadChats = await _messageRepository.GetUserChatsAsync(userId);
                foreach (var chat in unreadChats)
                {
                    if (chat.Messages.Any(m => m.ReceiverId == userId))
                    {
                        await Clients.User(userId).NewUnreadMessages(chat.OtherUserId);
                    }
                }

                // Check for unread group messages
                foreach (var group in userGroups)
                {
                    var groupHistory = await _groupRepository.GetGroupChatHistoryAsync(userId, group.GroupId);
                    if (groupHistory.UnreadCount > 0)
                    {
                        // Notify client about unread group messages
                        await Clients.User(userId).NewUnreadGroupMessages(group.GroupId);
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Notifies group members when a user leaves a group
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        public async Task LeaveGroupChat(Guid groupId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Remove user from SignalR group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");

                // Notify other members that this user has left
                await Clients.Group($"group_{groupId}").UserRemovedFromGroup(groupId, userId);

                _logger.LogInformation("User {UserId} left SignalR group {GroupId}", userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group chat");
                throw;
            }
        }
        public async Task NotifyUserRoleChanged(Guid groupId, string userId, GroupRole newRole)
        {
            try
            {
                var currentUserId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Notify the user whose role changed
                if (OnlineUsers.ContainsKey(userId))
                {
                    await Clients.User(userId).UserRoleChanged(groupId, userId, newRole);
                }

                // Notify group members
                await Clients.Group($"group_{groupId}").UserRoleChanged(groupId, userId, newRole);

                _logger.LogInformation("User {UserId} role changed to {NewRole} in group {GroupId} by {CurrentUserId}",
                    userId, newRole, groupId, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying role change");
                throw;
            }
        }

        public async Task NotifyGroupDeleted(Guid groupId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Notify all members in the SignalR group
                await Clients.Group($"group_{groupId}").GroupDeleted(groupId);                
                _logger.LogInformation("Group {GroupId} was deleted by {UserId}",groupId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying group deletion");
                throw;
            }        }

        #region Message Enhancements - Reactions & Mentions

        /// <summary>
        /// Adds a reaction to a message and notifies all relevant users
        /// </summary>
        /// <param name="createReactionDto">The reaction data containing messageId, reactionType, and messageType</param>
        public async Task ReactToMessage(CreateMessageReactionDTO createReactionDto)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Add reaction via repository
                var reactionDto = await _messageEnhancementRepository.AddReactionAsync(createReactionDto);

                // Determine notification scope based on message type
                if (createReactionDto.MessageType.Equals("group", StringComparison.OrdinalIgnoreCase))
                {
                    // Try group message
                    var groupMessage = await _groupRepository.GetGroupMessageByIdAsync(createReactionDto.MessageId);
                    if (groupMessage is not null)
                    {
                        // Notify all group members
                        await Clients.Group($"group_{groupMessage.GroupId}").MessageReactionAdded(reactionDto);
                    }
                }
                else
                {
                    // Direct message - get message details
                    var message = await _messageRepository.GetMessageByIdAsync(createReactionDto.MessageId);
                    if (message is not null)
                    {
                        // Notify sender and receiver
                        if (OnlineUsers.ContainsKey(message.SenderId))
                        {
                            await Clients.User(message.SenderId).MessageReactionAdded(reactionDto);
                        }
                        if (OnlineUsers.ContainsKey(message.ReceiverId))
                        {
                            await Clients.User(message.ReceiverId).MessageReactionAdded(reactionDto);
                        }
                    }
                }

                _logger.LogInformation("Reaction '{Reaction}' added to message {MessageId} by {UserId}",
                    createReactionDto.ReactionType, createReactionDto.MessageId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction to message {MessageId}", createReactionDto.MessageId);
                throw;
            }
        }        /// <summary>
        /// Removes a reaction from a message and notifies all relevant users
        /// </summary>
        /// <param name="removeReactionDto">The reaction removal data containing messageId, reactionType, and messageType</param>
        public async Task RemoveReactionFromMessage(RemoveMessageReactionDTO removeReactionDto)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Remove reaction via repository
                var success = await _messageEnhancementRepository.RemoveReactionAsync(removeReactionDto);
                
                if (success)
                {
                    // Determine notification scope based on message type
                    if (removeReactionDto.MessageType.Equals("group", StringComparison.OrdinalIgnoreCase))
                    {
                        // Try group message
                        var groupMessage = await _groupRepository.GetGroupMessageByIdAsync(removeReactionDto.MessageId);
                        if (groupMessage is not null)
                        {
                            // Notify all group members
                            await Clients.Group($"group_{groupMessage.GroupId}").MessageReactionRemoved(
                                removeReactionDto.MessageId, 
                                removeReactionDto.ReactionType, 
                                userId, 
                                removeReactionDto.MessageType);
                        }
                    }
                    else
                    {
                        // Direct message - get message details
                        var message = await _messageRepository.GetMessageByIdAsync(removeReactionDto.MessageId);
                        if (message is not null)
                        {
                            // Notify sender and receiver
                            if (OnlineUsers.ContainsKey(message.SenderId))
                            {
                                await Clients.User(message.SenderId).MessageReactionRemoved(
                                    removeReactionDto.MessageId, 
                                    removeReactionDto.ReactionType, 
                                    userId, 
                                    removeReactionDto.MessageType);
                            }
                            if (OnlineUsers.ContainsKey(message.ReceiverId))
                            {
                                await Clients.User(message.ReceiverId).MessageReactionRemoved(
                                    removeReactionDto.MessageId, 
                                    removeReactionDto.ReactionType, 
                                    userId, 
                                    removeReactionDto.MessageType);
                            }
                        }
                    }

                    _logger.LogInformation("Reaction '{Reaction}' removed from message {MessageId} by {UserId}",
                        removeReactionDto.ReactionType, removeReactionDto.MessageId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reaction from message {MessageId}", removeReactionDto.MessageId);
                throw;
            }
        }        
        /// <summary>
        /// Processes mentions in a message and sends notifications
        /// </summary>
        /// <param name="messageId">The ID of the message containing mentions</param>
        /// <param name="messageContent">The content of the message to parse for mentions</param>
        /// <param name="messageType">The type of message (direct or group)</param>
        /// <param name="groupId">The group ID if it's a group message</param>
        public async Task NotifyMentions(Guid messageId, string messageContent, string messageType, Guid? groupId = null)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Create mentions using the repository which will parse the content
                var mentions = await _messageEnhancementRepository.CreateMentionsAsync(messageId, messageContent, messageType, groupId);

                // Send real-time notifications for each mention
                foreach (var mention in mentions)
                {
                    if (OnlineUsers.ContainsKey(mention.MentionedUserId))
                    {
                        var notificationDto = new MentionNotificationDTO
                        {
                            MentionId = mention.MentionId,
                            MessageId = messageId,
                            MessageContent = messageContent,
                            MentionedByUserId = userId,
                            MentionedByUserName = mention.MentionedByUserName,
                            MentionedByUserAvatarUrl = null, // Will be populated by repository if needed
                            MessageType = messageType,
                            GroupId = groupId,
                            GroupName = null, // Will be populated by repository if needed
                            CreatedAt = mention.CreatedAt
                        };

                        await Clients.User(mention.MentionedUserId).MentionReceived(notificationDto);
                    }
                }

                _logger.LogInformation("Mentions processed for message {MessageId} by {UserId}. Mentioned users: {MentionedUsers}",
                    messageId, userId, string.Join(", ", mentions.Select(m => m.MentionedUserId)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mentions for message {MessageId}", messageId);
                throw;
            }
        }        
        /// <summary>
        /// Marks a mention as read
        /// </summary>
        /// <param name="mentionId">The ID of the mention</param>
        public async Task MarkMentionAsRead(Guid mentionId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User is not authenticated");
                }

                // Mark mention as read
                var success = await _messageEnhancementRepository.MarkMentionAsReadAsync(mentionId, userId);
                
                if (success)
                {
                    // Notify the user (for multi-device sync)
                    await Clients.User(userId).MentionRead(mentionId, userId);

                    _logger.LogInformation("Mention marked as read for mention {MentionId} by {UserId}",
                        mentionId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking mention as read for mention {MentionId}", mentionId);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parses a message content for @username mentions and returns the list of mentioned user IDs
        /// </summary>
        /// <param name="content">The message content to parse</param>
        /// <returns>List of user IDs that were mentioned</returns>
        private async Task<List<string>> ParseMentionsFromContent(string content)
        {
            var mentionedUserIds = new List<string>();
            
            if (string.IsNullOrEmpty(content))
                return mentionedUserIds;

            // Simple regex to find @username patterns
            var mentionPattern = @"@(\w+)";
            var matches = System.Text.RegularExpressions.Regex.Matches(content, mentionPattern);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var username = match.Groups[1].Value;
                
                // Look up user by username to get their ID
                // This assumes users have unique usernames stored in the database
                var user = await _messageRepository.GetUserByUsernameAsync(username);
                if (user is not null && !mentionedUserIds.Contains(user.Id))
                {
                    mentionedUserIds.Add(user.Id);
                }
            }

            return mentionedUserIds;
        }

        #endregion
    }
}
