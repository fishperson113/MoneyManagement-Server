namespace API.Models.Entities
{
    public class UserFriend
    {
        public string UserId { get; set; } = string.Empty;
        public string FriendId { get; set; } = string.Empty;
        public bool IsAccepted { get; set; } = false;
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }

        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser Friend { get; set; } = null!;
    }
}
