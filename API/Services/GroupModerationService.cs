using API.Data;
using API.Models.Entities;
using API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

/// <summary>
/// Interface for group moderation operations
/// </summary>
public interface IGroupModerationService
{
    Task<bool> CanModerateAsync(string userId, Guid groupId);
    Task<bool> MuteUserAsync(string moderatorId, Guid groupId, string targetUserId, string reason, TimeSpan duration);
    Task<bool> UnmuteUserAsync(string moderatorId, Guid groupId, string targetUserId);
    Task<bool> BanUserAsync(string moderatorId, Guid groupId, string targetUserId, string reason);
    Task<bool> UnbanUserAsync(string moderatorId, Guid groupId, string targetUserId);
    Task<bool> KickUserAsync(string moderatorId, Guid groupId, string targetUserId, string reason);
    Task<bool> DeleteMessageAsync(string moderatorId, Guid groupId, Guid messageId, string reason);
    Task<bool> GrantModRoleAsync(string adminId, Guid groupId, string targetUserId);
    Task<bool> RevokeModRoleAsync(string adminId, Guid groupId, string targetUserId);
    Task<List<GroupModerationAction>> GetModerationLogsAsync(Guid groupId, int page = 1, int pageSize = 20);
    Task<bool> IsUserMutedAsync(Guid groupId, string userId);
    Task<bool> IsUserBannedAsync(Guid groupId, string userId);
    Task<bool> IsMessageDeletedAsync(Guid messageId);
    Task<UserGroupStatusDTO?> GetUserGroupStatusAsync(Guid groupId, string userId);
}

/// <summary>
/// Service to handle group moderation operations
/// </summary>
public class GroupModerationService : IGroupModerationService
{
    private readonly ApplicationDbContext _context;

    public GroupModerationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Checks if a user has moderation privileges in a group
    /// </summary>
    public async Task<bool> CanModerateAsync(string userId, Guid groupId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null)
            return false;

        // Admin or Collaborator roles can moderate
        return member.Role == GroupRole.Admin || member.Role == GroupRole.Collaborator;
    }

    /// <summary>
    /// Mutes a user in a group for a specified duration
    /// </summary>
    public async Task<bool> MuteUserAsync(string moderatorId, Guid groupId, string targetUserId, string reason, TimeSpan duration)
    {
        if (!await CanModerateAsync(moderatorId, groupId))
            return false;

        var targetMember = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (targetMember == null)
            return false;

        // Check if moderator can moderate target (based on roles)
        if (!await CanModerateTarget(moderatorId, targetUserId, groupId))
            return false;

        // Create or update moderation status
        var moderation = await _context.Set<GroupMemberModeration>()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (moderation == null)
        {
            moderation = new GroupMemberModeration
            {
                GroupMemberId = targetMember.Id,
                UserId = targetUserId,
                GroupId = groupId,
                ModeratorId = moderatorId,
                IsMuted = true,
                MutedUntil = DateTime.UtcNow.Add(duration),
                MuteReason = reason
            };
            _context.Set<GroupMemberModeration>().Add(moderation);
        }
        else
        {
            moderation.IsMuted = true;
            moderation.MutedUntil = DateTime.UtcNow.Add(duration);
            moderation.MuteReason = reason;
            moderation.ModeratorId = moderatorId;
            moderation.UpdatedAt = DateTime.UtcNow;
        }

        // Log the action
        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = moderatorId,
            TargetUserId = targetUserId,
            ActionType = ModerationActionType.Mute,
            Reason = reason,
            ExpiresAt = moderation.MutedUntil
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Unmutes a user in a group
    /// </summary>
    public async Task<bool> UnmuteUserAsync(string moderatorId, Guid groupId, string targetUserId)
    {
        if (!await CanModerateAsync(moderatorId, groupId))
            return false;

        var moderation = await _context.Set<GroupMemberModeration>()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId && m.IsMuted);

        if (moderation == null)
            return false;

        moderation.IsMuted = false;
        moderation.MutedUntil = null;
        moderation.ModeratorId = moderatorId;
        moderation.UpdatedAt = DateTime.UtcNow;

        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = moderatorId,
            TargetUserId = targetUserId,
            ActionType = ModerationActionType.Unmute
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Bans a user from a group
    /// </summary>
    public async Task<bool> BanUserAsync(string moderatorId, Guid groupId, string targetUserId, string reason)
    {
        if (!await CanModerateAsync(moderatorId, groupId))
            return false;

        var targetMember = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (targetMember == null)
            return false;

        // Check if moderator can moderate target
        if (!await CanModerateTarget(moderatorId, targetUserId, groupId))
            return false;

        // Create or update moderation status
        var moderation = await _context.Set<GroupMemberModeration>()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (moderation == null)
        {
            moderation = new GroupMemberModeration
            {
                GroupMemberId = targetMember.Id,
                UserId = targetUserId,
                GroupId = groupId,
                ModeratorId = moderatorId,
                IsBanned = true,
                BanReason = reason
            };
            _context.Set<GroupMemberModeration>().Add(moderation);
        }
        else
        {
            moderation.IsBanned = true;
            moderation.BanReason = reason;
            moderation.ModeratorId = moderatorId;
            moderation.UpdatedAt = DateTime.UtcNow;
        }

        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = moderatorId,
            TargetUserId = targetUserId,
            ActionType = ModerationActionType.Ban,
            Reason = reason
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Unbans a user from a group
    /// </summary>
    public async Task<bool> UnbanUserAsync(string moderatorId, Guid groupId, string targetUserId)
    {
        if (!await CanModerateAsync(moderatorId, groupId))
            return false;

        var moderation = await _context.Set<GroupMemberModeration>()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId && m.IsBanned);

        if (moderation == null)
            return false;

        moderation.IsBanned = false;
        moderation.ModeratorId = moderatorId;
        moderation.UpdatedAt = DateTime.UtcNow;

        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = moderatorId,
            TargetUserId = targetUserId,
            ActionType = ModerationActionType.Unban
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Kicks a user from a group
    /// </summary>
    public async Task<bool> KickUserAsync(string moderatorId, Guid groupId, string targetUserId, string reason)
    {
        if (!await CanModerateAsync(moderatorId, groupId))
            return false;

        var targetMember = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (targetMember == null)
            return false;

        // Check if moderator can moderate target
        if (!await CanModerateTarget(moderatorId, targetUserId, groupId))
            return false;

        // Remove the member from the group
        _context.GroupMembers.Remove(targetMember);

        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = moderatorId,
            TargetUserId = targetUserId,
            ActionType = ModerationActionType.Kick,
            Reason = reason
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes (or marks as deleted) a message in a group
    /// </summary>
    public async Task<bool> DeleteMessageAsync(string moderatorId, Guid groupId, Guid messageId, string reason)
    {
        if (!await CanModerateAsync(moderatorId, groupId))
            return false;

        var message = await _context.GroupMessages
            .FirstOrDefaultAsync(m => m.MessageId == messageId && m.GroupId == groupId);

        if (message == null)
            return false;

        // Check if moderator can moderate message sender
        if (!await CanModerateTarget(moderatorId, message.SenderId, groupId))
            return false;

        // Create message moderation record
        var messageModeration = new GroupMessageModeration
        {
            GroupMessageId = messageId,
            GroupId = groupId,
            IsDeleted = true,
            DeletionReason = reason,
            ModeratorId = moderatorId
        };
        _context.Set<GroupMessageModeration>().Add(messageModeration);

        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = moderatorId,
            TargetUserId = message.SenderId,
            ActionType = ModerationActionType.DeleteMessage,
            MessageId = messageId,
            Reason = reason
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Grants moderator role (Collaborator) to a group member
    /// </summary>
    public async Task<bool> GrantModRoleAsync(string adminId, Guid groupId, string targetUserId)
    {
        // Only admins can grant mod permissions
        var admin = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == adminId && m.Role == GroupRole.Admin);

        if (admin == null)
            return false;

        var targetMember = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId && m.Role == GroupRole.Member);

        if (targetMember == null)
            return false;

        targetMember.Role = GroupRole.Collaborator;

        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = adminId,
            TargetUserId = targetUserId,
            ActionType = ModerationActionType.GrantModRole
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Revokes moderator role (Collaborator) from a group member
    /// </summary>
    public async Task<bool> RevokeModRoleAsync(string adminId, Guid groupId, string targetUserId)
    {
        // Only admins can revoke mod permissions
        var admin = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == adminId && m.Role == GroupRole.Admin);

        if (admin == null)
            return false;

        var targetMember = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId && m.Role == GroupRole.Collaborator);

        if (targetMember == null)
            return false;

        targetMember.Role = GroupRole.Member;

        var log = new GroupModerationAction
        {
            GroupId = groupId,
            ModeratorId = adminId,
            TargetUserId = targetUserId,
            ActionType = ModerationActionType.RevokeModRole
        };
        _context.Set<GroupModerationAction>().Add(log);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets moderation logs for a group
    /// </summary>
    public async Task<List<GroupModerationAction>> GetModerationLogsAsync(Guid groupId, int page = 1, int pageSize = 20)
    {
        return await _context.Set<GroupModerationAction>()
            .Where(log => log.GroupId == groupId)
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a user is currently muted in a group
    /// </summary>
    public async Task<bool> IsUserMutedAsync(Guid groupId, string userId)
    {
        var moderation = await _context.Set<GroupMemberModeration>()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId && m.IsMuted);

        if (moderation == null)
            return false;

        // Check if mute has expired
        if (moderation.MutedUntil.HasValue && moderation.MutedUntil.Value < DateTime.UtcNow)
        {
            moderation.IsMuted = false;
            moderation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a user is currently banned from a group
    /// </summary>
    public async Task<bool> IsUserBannedAsync(Guid groupId, string userId)
    {
        return await _context.Set<GroupMemberModeration>()
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId && m.IsBanned);
    }

    /// <summary>
    /// Checks if a message has been deleted by a moderator
    /// </summary>
    public async Task<bool> IsMessageDeletedAsync(Guid messageId)
    {
        return await _context.Set<GroupMessageModeration>()
            .AnyAsync(m => m.GroupMessageId == messageId && m.IsDeleted);
    }

    /// <summary>
    /// Helper method to check if a moderator can moderate a target user
    /// </summary>
    private async Task<bool> CanModerateTarget(string moderatorId, string targetUserId, Guid groupId)
    {
        if (moderatorId == targetUserId)
            return false; // Can't moderate yourself

        var moderator = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == moderatorId);

        var target = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (moderator == null || target == null)
            return false;

        // Admin can moderate anyone
        if (moderator.Role == GroupRole.Admin)
            return true;

        // Collaborator can only moderate Members, not other Collaborators or Admins
        if (moderator.Role == GroupRole.Collaborator)
            return target.Role == GroupRole.Member;

        return false;
    }

    /// <summary>
    /// Gets user group status
    /// </summary>
    public async Task<UserGroupStatusDTO?> GetUserGroupStatusAsync(Guid groupId, string userId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null)
            return null;

        var moderation = await _context.Set<GroupMemberModeration>()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        var isMuted = false;
        var mutedAt = (DateTime?)null;
        var mutedUntil = (DateTime?)null;
        var muteReason = (string?)null;

        if (moderation?.IsMuted == true)
        {
            // Check if mute has expired
            if (moderation.MutedUntil.HasValue && moderation.MutedUntil.Value < DateTime.UtcNow)
            {
                // Auto-unmute expired mute
                moderation.IsMuted = false;
                moderation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                isMuted = true;
                mutedAt = moderation.CreatedAt;
                mutedUntil = moderation.MutedUntil;
                muteReason = moderation.MuteReason;
            }
        }

        var isBanned = moderation?.IsBanned ?? false;
        var banReason = moderation?.BanReason;
        var lastModerationUpdate = moderation?.UpdatedAt;

        return new UserGroupStatusDTO
        {
            GroupId = groupId,
            UserId = userId,
            IsMuted = isMuted,
            IsBanned = isBanned,
            MutedAt = mutedAt,
            MutedUntil = mutedUntil,
            MuteReason = muteReason,
            BanReason = banReason,
            LastModerationUpdate = lastModerationUpdate
        };
    }
}