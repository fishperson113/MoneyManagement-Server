using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities;

/// <summary>
/// Represents the type of moderation action taken
/// </summary>
public enum ModerationActionType
{
    Mute = 0,
    Unmute = 1,
    Ban = 2,
    Unban = 3,
    Kick = 4,
    DeleteMessage = 5,
    GrantModRole = 6,
    RevokeModRole = 7
}

/// <summary>
/// Logs all moderation actions for audit and tracking purposes
/// </summary>
public class GroupModerationAction
{
    [Key]
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public string ModeratorId { get; set; } = null!;
    public ApplicationUser Moderator { get; set; } = null!;

    public string TargetUserId { get; set; } = null!;
    public ApplicationUser TargetUser { get; set; } = null!;

    public ModerationActionType ActionType { get; set; }
    public string? Reason { get; set; }

    public Guid? MessageId { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}