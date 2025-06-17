using API.Models.DTOs;

namespace API.Repositories;

/// <summary>
/// Repository interface for message enhancement features (reactions, mentions)
/// Safe extension: New repository that doesn't modify existing message repositories
/// </summary>
public interface IMessageEnhancementRepository
{
    // Message Reactions
    /// <summary>
    /// Adds a reaction to a message
    /// </summary>
    Task<MessageReactionDTO> AddReactionAsync(CreateMessageReactionDTO dto);

    /// <summary>
    /// Removes a reaction from a message
    /// </summary>
    Task<bool> RemoveReactionAsync(RemoveMessageReactionDTO dto);

    /// <summary>
    /// Gets all reactions for a specific message
    /// </summary>
    Task<MessageReactionSummaryDTO> GetMessageReactionsAsync(Guid messageId, string messageType);

    /// <summary>
    /// Gets all reactions for multiple messages (for chat history)
    /// </summary>
    Task<Dictionary<Guid, MessageReactionSummaryDTO>> GetMultipleMessageReactionsAsync(List<Guid> messageIds, string messageType);

    // Message Mentions
    /// <summary>
    /// Creates mentions for a message based on content parsing
    /// </summary>
    Task<List<MessageMentionDTO>> CreateMentionsAsync(Guid messageId, string messageContent, string messageType, Guid? groupId = null);

    /// <summary>
    /// Gets all mentions for a specific message
    /// </summary>
    Task<List<MessageMentionDTO>> GetMessageMentionsAsync(Guid messageId);

    /// <summary>
    /// Gets all unread mentions for a user
    /// </summary>
    Task<List<MentionNotificationDTO>> GetUnreadMentionsAsync(string userId);

    /// <summary>
    /// Marks a mention as read
    /// </summary>
    Task<bool> MarkMentionAsReadAsync(Guid mentionId, string userId);

    /// <summary>
    /// Marks all mentions as read for a user
    /// </summary>
    Task<int> MarkAllMentionsAsReadAsync(string userId);

    // Enhanced Messages
    /// <summary>
    /// Gets an enhanced message with reactions and mentions
    /// </summary>
    Task<EnhancedMessageDTO?> GetEnhancedMessageAsync(Guid messageId, string messageType);

    /// <summary>
    /// Gets enhanced messages for chat history
    /// </summary>
    Task<List<EnhancedMessageDTO>> GetEnhancedMessagesAsync(List<Guid> messageIds, string messageType);
}
