using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities;

/// <summary>
/// Represents moderation actions applied to group messages
/// </summary>
public class GroupMessageModeration
{
    [Key]
    public Guid Id { get; set; }

    public Guid GroupMessageId { get; set; }

    public Guid GroupId { get; set; }

    public bool IsDeleted { get; set; }
    public string? DeletionReason { get; set; }

    public string ModeratorId { get; set; } = null!;
    public ApplicationUser Moderator { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}