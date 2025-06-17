using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities;

/// <summary>
/// Entity for tracking message mentions (@username functionality)
/// Safe extension: New entity that doesn't modify existing Message entity
/// </summary>
public class MessageMention
{
    [Key]
    public Guid MentionId { get; set; }

    /// <summary>
    /// Reference to the message containing the mention
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// User who was mentioned
    /// </summary>
    public string MentionedUserId { get; set; } = null!;
    public ApplicationUser MentionedUser { get; set; } = null!;

    /// <summary>
    /// User who created the mention (sender of the message)
    /// </summary>
    public string MentionedByUserId { get; set; } = null!;
    public ApplicationUser MentionedByUser { get; set; } = null!;

    /// <summary>
    /// Position of the mention in the message content (for highlighting)
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Length of the mention text (e.g., "@john_doe" = 9 characters)
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Whether the mentioned user has seen this mention
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the mention was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this is for a direct message or group message
    /// Values: "direct", "group"
    /// </summary>
    public string MessageType { get; set; } = null!;

    /// <summary>
    /// For group messages, store the group ID
    /// </summary>
    public Guid? GroupId { get; set; }
}
