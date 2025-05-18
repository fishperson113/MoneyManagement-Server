// API/Models/DTOs/GroupDTOs.cs
using API.Models.Entities;

namespace API.Models.DTOs
{
    public class CreateGroupDTO
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public List<string>? InitialMemberIds { get; set; }
    }

    public class GroupDTO
    {
        public Guid GroupId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatorId { get; set; } = null!;
        public string CreatorName { get; set; } = null!;
        public int MemberCount { get; set; }
        public GroupRole Role { get; set; }
    }

    public class GroupMemberDTO
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public GroupRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class GroupMessageDTO
    {
        public Guid MessageId { get; set; }
        public Guid GroupId { get; set; }
        public string SenderId { get; set; } = null!;
        public string SenderName { get; set; } = null!;
        public string? SenderAvatarUrl { get; set; }
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
    }

    public class SendGroupMessageDTO
    {
        public required Guid GroupId { get; set; }
        public required string Content { get; set; }
    }

    public class GroupChatHistoryDTO
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = null!;
        public string? GroupImageUrl { get; set; }
        public List<GroupMessageDTO> Messages { get; set; } = new List<GroupMessageDTO>();
        public int UnreadCount { get; set; }
    }
    public class UpdateGroupDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
    public class AdminLeaveResult
    {
        public bool Success { get; set; }
        public string Action { get; set; } = null!;  // "leave" or "delete"
        public Guid GroupId { get; set; }
        public string? NewAdminId { get; set; }      // If action is "leave"
    }
}
