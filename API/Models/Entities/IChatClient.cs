using API.Models.DTOs;

namespace API.Models.Entities
{
    public interface IChatClient
    {
        Task ReceiveMessage(MessageDTO message);
        Task UserOnline(string userId);
        Task UserOffline(string userId);
        Task MessageRead(string messageId, string userId);
    }
}
