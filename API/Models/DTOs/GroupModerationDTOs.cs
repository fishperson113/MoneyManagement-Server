namespace API.Models.DTOs;

/// <summary>
/// Request to mute a user in a group
/// </summary>
public class MuteUserRequest
{
    public Guid GroupId { get; set; }
    public string TargetUserId { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public int DurationInMinutes { get; set; } = 60; // Default 1 hour
}

/// <summary>
/// Request to unmute, unban, grant or revoke permissions
/// </summary>
public class GroupUserActionRequest
{
    public Guid GroupId { get; set; }
    public string TargetUserId { get; set; } = null!;
}

/// <summary>
/// Request to ban or kick a user from a group
/// </summary>
public class BanKickUserRequest
{
    public Guid GroupId { get; set; }
    public string TargetUserId { get; set; } = null!;
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Request to delete a message
/// </summary>
public class DeleteMessageRequest
{
    public Guid GroupId { get; set; }
    public Guid MessageId { get; set; }
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Response containing moderation action details
/// </summary>
public class ModerationActionDTO
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string ModeratorId { get; set; } = null!;
    public string ModeratorName { get; set; } = null!;
    public string TargetUserId { get; set; } = null!;
    public string TargetUserName { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}