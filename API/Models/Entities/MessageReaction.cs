using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities;

/// <summary>
/// Entity for tracking message reactions (likes, hearts, etc.)
/// Safe extension: New entity that doesn't modify existing Message entity
/// </summary>
public class MessageReaction
{
    [Key]
    public Guid ReactionId { get; set; }

    /// <summary>
    /// Reference to the message being reacted to
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// User who made the reaction
    /// </summary>
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Type of reaction (emoji unicode or name)
    /// Examples: "👍", "❤️", "😂", "😮", "😢", "😡"
    /// </summary>
    public string ReactionType { get; set; } = null!;

    /// <summary>
    /// When the reaction was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this is for a direct message or group message
    /// Values: "direct", "group"
    /// </summary>
    public string MessageType { get; set; } = null!;
}
