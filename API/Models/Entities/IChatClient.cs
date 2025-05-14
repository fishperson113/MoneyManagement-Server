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

    }
}
