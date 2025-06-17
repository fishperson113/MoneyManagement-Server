using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace API.Repositories;

/// <summary>
/// Repository implementation for message enhancement features (reactions, mentions)
/// Safe extension: New repository that doesn't modify existing message repositories
/// </summary>
public class MessageEnhancementRepository : IMessageEnhancementRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<MessageEnhancementRepository> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MessageEnhancementRepository(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<MessageEnhancementRepository> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    #region Message Reactions

    public async Task<MessageReactionDTO> AddReactionAsync(CreateMessageReactionDTO dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Adding reaction {ReactionType} to message {MessageId} by user {UserId}",
                dto.ReactionType, dto.MessageId, userId);

            // Check if user already has this reaction on this message
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(mr => mr.MessageId == dto.MessageId &&
                                         mr.UserId == userId &&
                                         mr.ReactionType == dto.ReactionType);

            if (existingReaction != null)
            {
                // Reaction already exists, return existing
                return _mapper.Map<MessageReactionDTO>(existingReaction);
            }

            // Create new reaction
            var reaction = new MessageReaction
            {
                ReactionId = Guid.NewGuid(),
                MessageId = dto.MessageId,
                UserId = userId,
                ReactionType = dto.ReactionType,
                MessageType = dto.MessageType,
                CreatedAt = DateTime.UtcNow
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();

            // Get user details for DTO
            var user = await _context.Users.FindAsync(userId);
            var reactionDto = _mapper.Map<MessageReactionDTO>(reaction);
            reactionDto.UserName = user?.UserName ?? "Unknown";
            // reactionDto.UserAvatarUrl = user?.AvatarUrl; // If you have avatar URL property

            _logger.LogInformation("Successfully added reaction {ReactionId}", reaction.ReactionId);
            return reactionDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction to message {MessageId}", dto.MessageId);
            throw;
        }
    }

    public async Task<bool> RemoveReactionAsync(RemoveMessageReactionDTO dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Removing reaction {ReactionType} from message {MessageId} by user {UserId}",
                dto.ReactionType, dto.MessageId, userId);

            var reaction = await _context.MessageReactions
                .FirstOrDefaultAsync(mr => mr.MessageId == dto.MessageId &&
                                         mr.UserId == userId &&
                                         mr.ReactionType == dto.ReactionType);

            if (reaction == null)
            {
                _logger.LogWarning("Reaction not found for removal");
                return false;
            }

            _context.MessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully removed reaction {ReactionId}", reaction.ReactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from message {MessageId}", dto.MessageId);
            throw;
        }
    }

    public async Task<MessageReactionSummaryDTO> GetMessageReactionsAsync(Guid messageId, string messageType)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var reactions = await _context.MessageReactions
                .Include(mr => mr.User)
                .Where(mr => mr.MessageId == messageId && mr.MessageType == messageType)
                .ToListAsync();

            var summary = new MessageReactionSummaryDTO
            {
                MessageId = messageId
            };

            // Group reactions by type and count them
            var reactionGroups = reactions.GroupBy(r => r.ReactionType);
            
            foreach (var group in reactionGroups)
            {
                var reactionType = group.Key;
                var reactionList = group.ToList();
                
                summary.ReactionCounts[reactionType] = reactionList.Count;
                summary.ReactionDetails[reactionType] = _mapper.Map<List<MessageReactionDTO>>(reactionList);
            }

            // Check if current user has reacted
            var userReactions = reactions.Where(r => r.UserId == userId).ToList();
            summary.HasUserReacted = userReactions.Any();
            summary.UserReactionTypes = userReactions.Select(r => r.ReactionType).ToList();

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reactions for message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<Dictionary<Guid, MessageReactionSummaryDTO>> GetMultipleMessageReactionsAsync(List<Guid> messageIds, string messageType)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var reactions = await _context.MessageReactions
                .Include(mr => mr.User)
                .Where(mr => messageIds.Contains(mr.MessageId) && mr.MessageType == messageType)
                .ToListAsync();

            var result = new Dictionary<Guid, MessageReactionSummaryDTO>();

            foreach (var messageId in messageIds)
            {
                var messageReactions = reactions.Where(r => r.MessageId == messageId).ToList();
                
                var summary = new MessageReactionSummaryDTO
                {
                    MessageId = messageId
                };

                var reactionGroups = messageReactions.GroupBy(r => r.ReactionType);
                
                foreach (var group in reactionGroups)
                {
                    var reactionType = group.Key;
                    var reactionList = group.ToList();
                    
                    summary.ReactionCounts[reactionType] = reactionList.Count;
                    summary.ReactionDetails[reactionType] = _mapper.Map<List<MessageReactionDTO>>(reactionList);
                }

                var userReactions = messageReactions.Where(r => r.UserId == userId).ToList();
                summary.HasUserReacted = userReactions.Any();
                summary.UserReactionTypes = userReactions.Select(r => r.ReactionType).ToList();

                result[messageId] = summary;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reactions for multiple messages");
            throw;
        }
    }

    #endregion

    #region Message Mentions

    public async Task<List<MessageMentionDTO>> CreateMentionsAsync(Guid messageId, string messageContent, string messageType, Guid? groupId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Creating mentions for message {MessageId}", messageId);

            // Parse mentions from message content using regex
            var mentionRegex = new Regex(@"@(\w+)", RegexOptions.IgnoreCase);
            var matches = mentionRegex.Matches(messageContent);

            var createdMentions = new List<MessageMention>();

            foreach (Match match in matches)
            {
                var username = match.Groups[1].Value;
                
                // Find user by username
                var mentionedUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == username);

                if (mentionedUser != null && mentionedUser.Id != userId) // Don't mention yourself
                {
                    // Check if mention already exists
                    var existingMention = await _context.MessageMentions
                        .FirstOrDefaultAsync(mm => mm.MessageId == messageId &&
                                                 mm.MentionedUserId == mentionedUser.Id);

                    if (existingMention == null)
                    {
                        var mention = new MessageMention
                        {
                            MentionId = Guid.NewGuid(),
                            MessageId = messageId,
                            MentionedUserId = mentionedUser.Id,
                            MentionedByUserId = userId,
                            StartPosition = match.Index,
                            Length = match.Length,
                            MessageType = messageType,
                            GroupId = groupId,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.MessageMentions.Add(mention);
                        createdMentions.Add(mention);
                    }
                }
            }

            if (createdMentions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} mentions for message {MessageId}", 
                    createdMentions.Count, messageId);
            }

            return _mapper.Map<List<MessageMentionDTO>>(createdMentions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mentions for message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<List<MessageMentionDTO>> GetMessageMentionsAsync(Guid messageId)
    {
        try
        {
            var mentions = await _context.MessageMentions
                .Include(mm => mm.MentionedUser)
                .Include(mm => mm.MentionedByUser)
                .Where(mm => mm.MessageId == messageId)
                .ToListAsync();

            return _mapper.Map<List<MessageMentionDTO>>(mentions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentions for message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<List<MentionNotificationDTO>> GetUnreadMentionsAsync(string userId)
    {
        try
        {
            var mentions = await _context.MessageMentions
                .Include(mm => mm.MentionedByUser)
                .Where(mm => mm.MentionedUserId == userId && !mm.IsRead)
                .OrderByDescending(mm => mm.CreatedAt)
                .ToListAsync();

            var notifications = new List<MentionNotificationDTO>();

            foreach (var mention in mentions)
            {
                // Get message content based on message type
                string messageContent = "";
                string? groupName = null;

                if (mention.MessageType == "direct")
                {
                    var message = await _context.Messages
                        .FirstOrDefaultAsync(m => m.MessageID == mention.MessageId);
                    messageContent = message?.Content ?? "";
                }
                else if (mention.MessageType == "group")
                {
                    var groupMessage = await _context.GroupMessages
                        .Include(gm => gm.Group)
                        .FirstOrDefaultAsync(gm => gm.MessageId == mention.MessageId);
                    messageContent = groupMessage?.Content ?? "";
                    groupName = groupMessage?.Group?.Name;
                }

                var notification = new MentionNotificationDTO
                {
                    MentionId = mention.MentionId,
                    MessageId = mention.MessageId,
                    MessageContent = messageContent,
                    MentionedByUserId = mention.MentionedByUserId,
                    MentionedByUserName = mention.MentionedByUser.UserName ?? "Unknown",
                    // MentionedByUserAvatarUrl = mention.MentionedByUser.AvatarUrl, // If available
                    MessageType = mention.MessageType,
                    GroupId = mention.GroupId,
                    GroupName = groupName,
                    CreatedAt = mention.CreatedAt
                };

                notifications.Add(notification);
            }

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread mentions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> MarkMentionAsReadAsync(Guid mentionId, string userId)
    {
        try
        {
            var mention = await _context.MessageMentions
                .FirstOrDefaultAsync(mm => mm.MentionId == mentionId && mm.MentionedUserId == userId);

            if (mention == null)
            {
                return false;
            }

            mention.IsRead = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked mention {MentionId} as read for user {UserId}", mentionId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking mention {MentionId} as read", mentionId);
            throw;
        }
    }

    public async Task<int> MarkAllMentionsAsReadAsync(string userId)
    {
        try
        {
            var unreadMentions = await _context.MessageMentions
                .Where(mm => mm.MentionedUserId == userId && !mm.IsRead)
                .ToListAsync();

            foreach (var mention in unreadMentions)
            {
                mention.IsRead = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} mentions as read for user {UserId}", unreadMentions.Count, userId);
            return unreadMentions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all mentions as read for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Enhanced Messages

    public async Task<EnhancedMessageDTO?> GetEnhancedMessageAsync(Guid messageId, string messageType)
    {
        try
        {
            // This would need to be implemented based on your specific message entities
            // For now, returning null as this requires integration with existing message repositories
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<List<EnhancedMessageDTO>> GetEnhancedMessagesAsync(List<Guid> messageIds, string messageType)
    {
        try
        {
            // This would need to be implemented based on your specific message entities
            // For now, returning empty list as this requires integration with existing message repositories
            await Task.CompletedTask;
            return new List<EnhancedMessageDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced messages");
            throw;
        }
    }

    #endregion
}
