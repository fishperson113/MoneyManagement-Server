namespace API.Models.DTOs;

/// <summary>
/// DTO for message reaction data
/// </summary>
public class MessageReactionDTO
{
    public Guid ReactionId { get; set; }
    public Guid MessageId { get; set; }
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public string ReactionType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string MessageType { get; set; } = null!;
}

/// <summary>
/// DTO for creating a new reaction
/// </summary>
public class CreateMessageReactionDTO
{
    public Guid MessageId { get; set; }
    public string ReactionType { get; set; } = null!;
    public string MessageType { get; set; } = null!; // "direct" or "group"
}

/// <summary>
/// DTO for removing a reaction
/// </summary>
public class RemoveMessageReactionDTO
{
    public Guid MessageId { get; set; }
    public string ReactionType { get; set; } = null!;
    public string MessageType { get; set; } = null!;
}

/// <summary>
/// DTO for reaction summary (count by type)
/// </summary>
public class MessageReactionSummaryDTO
{
    public Guid MessageId { get; set; }
    public Dictionary<string, int> ReactionCounts { get; set; } = new();
    public Dictionary<string, List<MessageReactionDTO>> ReactionDetails { get; set; } = new();
    public bool HasUserReacted { get; set; }
    public List<string> UserReactionTypes { get; set; } = new();
}

/// <summary>
/// DTO for mention data
/// </summary>
public class MessageMentionDTO
{
    public Guid MentionId { get; set; }
    public Guid MessageId { get; set; }
    public string MentionedUserId { get; set; } = null!;
    public string MentionedUserName { get; set; } = null!;
    public string? MentionedUserAvatarUrl { get; set; }
    public string MentionedByUserId { get; set; } = null!;
    public string MentionedByUserName { get; set; } = null!;
    public int StartPosition { get; set; }
    public int Length { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MessageType { get; set; } = null!;
    public Guid? GroupId { get; set; }
}

/// <summary>
/// Enhanced message DTO with reactions and mentions
/// Safe extension: New DTO that doesn't modify existing MessageDTO
/// </summary>
public class EnhancedMessageDTO
{
    public Guid MessageID { get; set; }
    public string SenderId { get; set; } = null!;
    public string SenderName { get; set; } = null!;
    public string? SenderAvatarUrl { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public string MessageType { get; set; } = null!; // "direct" or "group"
    
    // For direct messages
    public string? ReceiverId { get; set; }
    public string? ReceiverName { get; set; }
    
    // For group messages
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    
    // Enhanced features
    public MessageReactionSummaryDTO Reactions { get; set; } = new();
    public List<MessageMentionDTO> Mentions { get; set; } = new();
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }
}

/// <summary>
/// DTO for mention notification
/// </summary>
public class MentionNotificationDTO
{
    public Guid MentionId { get; set; }
    public Guid MessageId { get; set; }
    public string MessageContent { get; set; } = null!;
    public string MentionedByUserId { get; set; } = null!;
    public string MentionedByUserName { get; set; } = null!;
    public string? MentionedByUserAvatarUrl { get; set; }
    public string MessageType { get; set; } = null!;
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public DateTime CreatedAt { get; set; }
}
