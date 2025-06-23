using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities;

/// <summary>
/// Represents the moderation status of a group member
/// </summary>
public class GroupMemberModeration
{
    [Key]
    public Guid Id { get; set; }

    public Guid GroupMemberId { get; set; }
    public GroupMember GroupMember { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public Guid GroupId { get; set; }

    public bool IsMuted { get; set; }
    public bool IsBanned { get; set; }

    public DateTime? MutedUntil { get; set; }
    public string? MuteReason { get; set; }
    public string? BanReason { get; set; }

    public string ModeratorId { get; set; } = null!;
    public ApplicationUser Moderator { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}