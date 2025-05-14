using API.Hub;
using API.Models.Entities;
using Microsoft.AspNetCore.SignalR;

namespace API.Services
{
    public interface IUserProfileMediator
    {
        Task NotifyAvatarChanged(string userId, string newAvatarUrl);
        void Subscribe(string connectionId, string userId);
        void Unsubscribe(string connectionId);
    }

    public class UserProfileMediator : IUserProfileMediator
    {
        private readonly IHubContext<ChatHub, IChatClient> _hubContext;

        public UserProfileMediator(IHubContext<ChatHub, IChatClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyAvatarChanged(string userId, string newAvatarUrl)
        {
            await _hubContext.Clients.All.UserAvatarUpdated(userId, newAvatarUrl);
        }

        public void Subscribe(string connectionId, string userId)
        {
        }

        public void Unsubscribe(string connectionId)
        {
        }
    }
}
