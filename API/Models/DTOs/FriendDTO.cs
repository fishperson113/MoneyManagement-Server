namespace API.Models.DTOs
{
    public class FriendDTO
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string DisplayName { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastActive { get; set; }
        public bool IsPendingRequest { get; set; }
    }

    public class FriendRequestDTO
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string DisplayName { get; set; }
        public required DateTime RequestedAt { get; set; }
    }

    public class AddFriendDTO
    {
        public required string FriendId { get; set; }
    }
}
