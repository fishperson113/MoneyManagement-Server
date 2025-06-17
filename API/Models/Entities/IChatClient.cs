using API.Models.DTOs;

namespace API.Models.Entities
{
    public interface IChatClient
    {
        Task ReceiveMessage(MessageDTO message);
        Task UserOnline(string userId);
        Task UserOffline(string userId);
        Task MessageRead(string messageId, string userId);
        Task NewUnreadMessages(string fromUserId);
        Task FriendRequestReceived(FriendRequestDTO request);
        Task FriendRequestAccepted(string userId);
        Task UserAvatarUpdated(string userId, string newAvatarUrl);
        Task ReceiveGroupMessage(GroupMessageDTO message);
        Task GroupMessagesRead(Guid groupId, string userId);
        Task UserAddedToGroup(GroupDTO group);
        Task UserRemovedFromGroup(Guid groupId, string userId);
        Task NewUnreadGroupMessages(Guid groupId);        
        Task UserRoleChanged(Guid groupId, string userId, GroupRole newRole);
        Task GroupDeleted(Guid groupId);
        
        // Safe extensions: New methods for message enhancements
        /// <summary>
        /// Notifies clients when a message receives a new reaction
        /// </summary>
        Task MessageReactionAdded(MessageReactionDTO reaction);
        
        /// <summary>
        /// Notifies clients when a message reaction is removed
        /// </summary>
        Task MessageReactionRemoved(Guid messageId, string reactionType, string userId, string messageType);
        
        /// <summary>
        /// Notifies a user when they are mentioned in a message
        /// </summary>
        Task MentionReceived(MentionNotificationDTO mention);
        
        /// <summary>
        /// Notifies clients when mention read status changes
        /// </summary>
        Task MentionRead(Guid mentionId, string userId);
    }
}
