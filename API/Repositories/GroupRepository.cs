// API/Repositories/GroupRepository.cs
using API.Data;
using API.Exceptions;
using API.Helpers;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Repositories
{
    public class GroupRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<GroupRepository> _logger;
        private readonly FirestoreDb _firestoreDb;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GroupRepository(
            ApplicationDbContext dbContext,
            IMapper mapper,
            ILogger<GroupRepository> logger,
            FirestoreDb firestoreDb,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _firestoreDb = firestoreDb;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User is not authenticated");
        }

        public async Task<GroupDTO> CreateGroupAsync(string creatorId, CreateGroupDTO dto)
        {
            try
            {
                _logger.LogInformation("Creating group {GroupName} by user {UserId}", dto.Name, creatorId);

                // Create and map the group
                var group = new Group
                {
                    GroupId = Guid.NewGuid(),
                    CreatorId = creatorId,
                    CreatedAt = DateTime.UtcNow,
                    Name = dto.Name,
                    Description = dto.Description
                };

                _dbContext.Groups.Add(group);

                // Add creator as first admin member
                var creatorMember = new GroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.GroupId,
                    UserId = creatorId,
                    Role = GroupRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };

                _dbContext.GroupMembers.Add(creatorMember);

                // Add initial members if provided
                if (dto.InitialMemberIds != null && dto.InitialMemberIds.Count > 0)
                {
                    foreach (var memberId in dto.InitialMemberIds.Where(id => id != creatorId))
                    {
                        var member = new GroupMember
                        {
                            Id = Guid.NewGuid(),
                            GroupId = group.GroupId,
                            UserId = memberId,
                            Role = GroupRole.Admin,
                            JoinedAt = DateTime.UtcNow
                        };

                        _dbContext.GroupMembers.Add(member);
                    }
                }

                await _dbContext.SaveChangesAsync();

                // Create group in Firestore for real-time access
                await _firestoreDb.Collection("groups")
                    .Document(group.GroupId.ToString())
                    .SetAsync(new
                    {
                        groupId = group.GroupId.ToString(),
                        name = group.Name,
                        description = group.Description,
                        creatorId = group.CreatorId,
                        createdAt = group.CreatedAt
                    });

                // Load creator info for response
                await _dbContext.Entry(group).Reference(g => g.Creator).LoadAsync();

                // Get the member count for the DTO
                var memberCount = await _dbContext.GroupMembers
                    .CountAsync(m => m.GroupId == group.GroupId);

                // Map to DTO with additional properties
                var groupDto = _mapper.Map<GroupDTO>(group);
                groupDto.CreatorName = $"{group.Creator.FirstName} {group.Creator.LastName}";
                groupDto.MemberCount = memberCount;
                groupDto.Role = GroupRole.Admin; // Creator is always admin

                return groupDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                throw;
            }
        }

        public async Task<GroupMessageDTO> SendGroupMessageAsync(string senderId, SendGroupMessageDTO dto)
        {
            try
            {
                // Before creating the message, check if user can send messages
                if (!await CanSendMessagesAsync(dto.GroupId, senderId))
                {
                    throw new UserMutedOrBannedException();
                }

                _logger.LogInformation("Sending group message from {SenderId} to group {GroupId}", senderId, dto.GroupId);

                // Verify user is a member of the group
                var isMember = await _dbContext.GroupMembers
                    .AnyAsync(m => m.GroupId == dto.GroupId && m.UserId == senderId);

                if (!isMember)
                    throw new UnauthorizedAccessException("User is not a member of this group");

                // Verify the group exists
                var groupExists = await _dbContext.Groups.AnyAsync(g => g.GroupId == dto.GroupId);
                if (!groupExists)
                    throw new KeyNotFoundException($"Group with ID {dto.GroupId} not found");

                // Validate message content
                if (string.IsNullOrWhiteSpace(dto.Content))
                    throw new ArgumentException("Message content cannot be empty", nameof(dto.Content));

                // Create message using AutoMapper
                var message = _mapper.Map<GroupMessage>(dto);
                message.MessageId = Guid.NewGuid();
                message.SenderId = senderId;
                message.SentAt = DateTime.UtcNow;

                _dbContext.GroupMessages.Add(message);
                await _dbContext.SaveChangesAsync();

                // Store in Firestore for real-time functionality
                await _firestoreDb.Collection("groups")
                    .Document(dto.GroupId.ToString())
                    .Collection("messages")
                    .Document(message.MessageId.ToString())
                    .SetAsync(new
                    {
                        messageId = message.MessageId.ToString(),
                        groupId = message.GroupId.ToString(),
                        senderId = message.SenderId,
                        content = message.Content,
                        sentAt = message.SentAt
                    });

                // Get sender info directly from the database
                var sender = await _dbContext.Users.FindAsync(senderId);
                if (sender == null)
                {
                    throw new KeyNotFoundException($"User with ID {senderId} not found");
                }

                // Map to DTO with sender details
                var messageDto = _mapper.Map<GroupMessageDTO>(message);
                messageDto.SenderName = $"{sender.FirstName} {sender.LastName}".Trim();
                messageDto.SenderAvatarUrl = sender.AvatarUrl;

                return messageDto;
            }
            catch (UserMutedOrBannedException)
            {
                _logger.LogWarning("User {UserId} attempted to send message while muted/banned in group {GroupId}",
                    senderId, dto.GroupId);
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User {UserId} is not a member of group {GroupId}", senderId, dto.GroupId);
                throw;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Group {GroupId} or user {UserId} not found when sending message",
                    dto.GroupId, senderId);
                throw;
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid message content from user {UserId} to group {GroupId}",
                    senderId, dto.GroupId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending group message from {SenderId} to group {GroupId}",
                    senderId, dto.GroupId);
                throw; // Rethrow to preserve the stack trace
            }
        }

        public async Task<GroupMessage?> GetGroupMessageByIdAsync(Guid messageId)
        {
            try
            {
                return await _dbContext.GroupMessages
                    .FirstOrDefaultAsync(m => m.MessageId == messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group message by ID {MessageId}", messageId);
                throw;
            }
        }

        public async Task<List<GroupDTO>> GetUserGroupsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting groups for user {UserId}", userId);

                var groupMembers = await _dbContext.GroupMembers
                    .Where(m => m.UserId == userId)
                    .Include(m => m.Group)
                    .ThenInclude(g => g.Creator)
                    .Select(m => new { GroupMember = m, Group = m.Group })
                    .ToListAsync();

                var groups = new List<GroupDTO>();

                foreach (var item in groupMembers)
                {
                    var group = _mapper.Map<GroupDTO>(item.Group);

                    // Set additional properties that aren't auto-mapped
                    group.CreatorName = $"{item.Group.Creator.FirstName} {item.Group.Creator.LastName}";
                    group.MemberCount = await _dbContext.GroupMembers.CountAsync(m => m.GroupId == item.Group.GroupId);
                    group.Role = item.GroupMember.Role;

                    groups.Add(group);
                }

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user groups");
                throw;
            }
        }

        public async Task<GroupChatHistoryDTO> GetGroupChatHistoryAsync(string userId, Guid groupId)
        {
            try
            {
                _logger.LogInformation("Getting chat history for group {GroupId} for user {UserId}", groupId, userId);

                // Verify user is a member of the group
                var membership = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (membership == null)
                    throw new UnauthorizedAccessException("User is not a member of this group");

                // Get group details
                var group = await _dbContext.Groups
                    .FirstOrDefaultAsync(g => g.GroupId == groupId);

                if (group == null)
                    throw new KeyNotFoundException($"Group with ID {groupId} not found");

                var messagesQuery = _dbContext.GroupMessages
                    .Where(m => m.GroupId == groupId)
                    .Include(m => m.Sender)
                    .OrderBy(m => m.SentAt)
                    .Take(100);

                var messages = await FilterDeletedMessagesAsync(messagesQuery);


                // Map to DTOs
                var messagesDto = _mapper.Map<List<GroupMessageDTO>>(messages);

                // Fill in sender details for each message
                for (int i = 0; i < messages.Count; i++)
                {
                    messagesDto[i].SenderName = $"{messages[i].Sender.FirstName} {messages[i].Sender.LastName}";
                    messagesDto[i].SenderAvatarUrl = messages[i].Sender.AvatarUrl;
                }

                // Calculate unread messages
                int unreadCount = 0;
                if (membership.LastReadTime.HasValue)
                {
                    unreadCount = messages.Count(m => m.SentAt > membership.LastReadTime.Value && m.SenderId != userId);
                }
                else
                {
                    unreadCount = messages.Count(m => m.SenderId != userId);
                }

                // Create DTO for response
                var chatHistory = new GroupChatHistoryDTO
                {
                    GroupId = group.GroupId,
                    GroupName = group.Name,
                    GroupImageUrl = group.ImageUrl,
                    Messages = messagesDto,
                    UnreadCount = unreadCount
                };

                return chatHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group chat history");
                throw;
            }
        }

        public async Task<bool> MarkGroupMessagesAsReadAsync(string userId, Guid groupId)
        {
            try
            {
                _logger.LogInformation("Marking messages as read in group {GroupId} for user {UserId}", groupId, userId);

                var membership = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (membership == null)
                    return false;

                // Update last read time
                membership.LastReadTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // Update in Firestore
                await _firestoreDb.Collection("groups")
                    .Document(groupId.ToString())
                    .Collection("readReceipts")
                    .Document(userId)
                    .SetAsync(new
                    {
                        userId = userId,
                        lastReadTime = membership.LastReadTime
                    });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking group messages as read");
                return false;
            }
        }

        public async Task<List<GroupMemberDTO>> GetGroupMembersAsync(string userId, Guid groupId)
        {
            try
            {
                _logger.LogInformation("Getting members for group {GroupId}", groupId);

                // Verify user is a member of the group
                var isMember = await _dbContext.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (!isMember)
                    throw new UnauthorizedAccessException("User is not a member of this group");

                // Get members with user details
                var members = await _dbContext.GroupMembers
                    .Where(m => m.GroupId == groupId)
                    .Include(m => m.User)
                    .ToListAsync();

                // Map to DTOs and add display name
                var membersDto = _mapper.Map<List<GroupMemberDTO>>(members);

                // Update display names from user data
                for (int i = 0; i < members.Count; i++)
                {
                    membersDto[i].DisplayName = $"{members[i].User.FirstName} {members[i].User.LastName}";
                    membersDto[i].AvatarUrl = members[i].User.AvatarUrl;
                }

                return membersDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group members");
                throw;
            }
        }

        public async Task<bool> AddUserToGroupAsync(string adminUserId, Guid groupId, string newUserId)
        {
            try
            {
                _logger.LogInformation("Adding user {NewUserId} to group {GroupId} by admin {AdminUserId}",
                    newUserId, groupId, adminUserId);

                // Verify the current user is an admin or collaborator of the group
                var adminMember = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == adminUserId);

                if (adminMember == null || adminMember.Role < GroupRole.Collaborator)
                    throw new UnauthorizedAccessException("Only group admins or collaborators can add members");

                // Check if user is already a member
                var isAlreadyMember = await _dbContext.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == newUserId);

                if (isAlreadyMember)
                    return false; // Already a member

                // Add the new member
                var member = new GroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = newUserId,
                    Role = GroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                };

                _dbContext.GroupMembers.Add(member);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to group");
                throw;
            }
        }


        public async Task<bool> RemoveUserFromGroupAsync(string adminUserId, Guid groupId, string userToRemoveId)
        {
            try
            {
                _logger.LogInformation("Removing user {UserToRemoveId} from group {GroupId} by user {AdminUserId}",
                    userToRemoveId, groupId, adminUserId);

                // Special case: user removing themselves (leaving group)
                bool isSelfRemoval = adminUserId == userToRemoveId;

                // Find the membership to remove
                var memberToRemove = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userToRemoveId);

                if (memberToRemove == null)
                    return false; // Not a member

                // If not self-removal, check if admin
                if (!isSelfRemoval)
                {
                    // Verify the current user is an admin or collaborator of the group
                    var adminMember = await _dbContext.GroupMembers
                        .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == adminUserId);

                    if (adminMember == null || adminMember.Role < GroupRole.Collaborator)
                        throw new UnauthorizedAccessException("Only group admins or collaborators can remove members");

                    // Collaborators can't remove admins
                    if (adminMember.Role == GroupRole.Collaborator && memberToRemove.Role == GroupRole.Admin)
                        throw new UnauthorizedAccessException("Collaborators cannot remove admins");
                }

                // Check if removing the last admin (only applies to admin self-removal)
                if (memberToRemove.Role == GroupRole.Admin && isSelfRemoval)
                {
                    var adminCount = await _dbContext.GroupMembers
                        .CountAsync(m => m.GroupId == groupId && m.Role == GroupRole.Admin);

                    if (adminCount <= 1)
                        throw new InvalidOperationException("As the last admin, you must use the admin leave process");
                }

                // Remove the member
                _dbContext.GroupMembers.Remove(memberToRemove);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from group");
                throw;
            }
        }


        public async Task<bool> UpdateGroupAsync(string adminUserId, Guid groupId, UpdateGroupDTO dto)
        {
            try
            {
                _logger.LogInformation("Updating group {GroupId} by admin {AdminUserId}",
                    groupId, adminUserId);

                // Verify the current user is an admin of the group
                var isAdmin = await _dbContext.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == adminUserId && m.Role == GroupRole.Admin);
                if (!isAdmin)
                    throw new UnauthorizedAccessException("Only group admins can update group");

                // Find the group
                var group = await _dbContext.Groups.FindAsync(groupId);

                if (group == null)
                    throw new KeyNotFoundException($"Group with ID {groupId} not found");

                // Update properties
                if (!string.IsNullOrWhiteSpace(dto.Name))
                    group.Name = dto.Name;

                if (dto.Description != null) // Allow empty string to clear description
                    group.Description = dto.Description;

                await _dbContext.SaveChangesAsync();

                // Update in Firestore
                await _firestoreDb.Collection("groups")
                    .Document(groupId.ToString())
                    .UpdateAsync(new Dictionary<string, object>
                    {
                        { "name", group.Name },
                        { "description", group.Description ?? "" }
                    });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                throw;
            }
        }
        /// <summary>
        /// Assigns a collaborator role to a group member
        /// </summary>
        public async Task<bool> AssignCollaboratorRoleAsync(string adminUserId, Guid groupId, string userId)
        {
            try
            {
                _logger.LogInformation("Assigning collaborator role in group {GroupId} to user {UserId} by admin {AdminId}",
                    groupId, userId, adminUserId);

                // Verify the current user is an admin of the group
                var adminMember = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == adminUserId);

                if (adminMember == null || adminMember.Role != GroupRole.Admin)
                    throw new UnauthorizedAccessException("Only group admins can assign roles");

                // Find the member to promote
                var member = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (member == null)
                    throw new KeyNotFoundException($"User {userId} is not a member of this group");

                // Assign collaborator role
                member.Role = GroupRole.Collaborator;
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning collaborator role");
                throw;
            }
        }

        /// <summary>
        /// Deletes a group entirely
        /// </summary>
        public async Task<bool> DeleteGroupAsync(string adminUserId, Guid groupId)
        {
            try
            {
                _logger.LogInformation("Deleting group {GroupId} by admin {AdminId}", groupId, adminUserId);

                // Verify the current user is an admin of the group
                var adminMember = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == adminUserId);

                if (adminMember == null || adminMember.Role != GroupRole.Admin)
                    throw new UnauthorizedAccessException("Only group admins can delete groups");

                // Find the group
                var group = await _dbContext.Groups
                    .Include(g => g.Members)
                    .Include(g => g.Messages)
                    .FirstOrDefaultAsync(g => g.GroupId == groupId);

                if (group == null)
                    throw new KeyNotFoundException($"Group with ID {groupId} not found");

                // Begin transaction since we're deleting multiple related entities
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Remove all messages
                    _dbContext.GroupMessages.RemoveRange(group.Messages);

                    // Remove all memberships
                    _dbContext.GroupMembers.RemoveRange(group.Members);

                    // Remove the group
                    _dbContext.Groups.Remove(group);

                    await _dbContext.SaveChangesAsync();

                    // Also delete from Firestore
                    await _firestoreDb.Collection("groups")
                        .Document(groupId.ToString())
                        .DeleteAsync();

                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group");
                throw;
            }
        }

        /// <summary>
        /// Handles admin leaving a group with automatic role succession
        /// </summary>
        public async Task<AdminLeaveResult> AdminLeaveGroupAsync(string adminUserId, Guid groupId, bool deleteGroup)
        {
            try
            {
                _logger.LogInformation("Admin {AdminId} leaving group {GroupId}, deleteGroup={DeleteGroup}",
                    adminUserId, groupId, deleteGroup);

                // Verify user is an admin
                var adminMember = await _dbContext.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == adminUserId);

                if (adminMember == null)
                    throw new KeyNotFoundException("User is not a member of this group");

                if (adminMember.Role != GroupRole.Admin)
                    throw new UnauthorizedAccessException("Only admins can use this function");

                // If delete option is selected, delete the group entirely
                if (deleteGroup)
                {
                    var deleted = await DeleteGroupAsync(adminUserId, groupId);
                    return new AdminLeaveResult
                    {
                        Success = deleted,
                        Action = "delete",
                        GroupId = groupId
                    };
                }

                // Find next admin based on role precedence and join time
                var nextAdmin = await _dbContext.GroupMembers
                    .Where(m => m.GroupId == groupId && m.UserId != adminUserId)
                    .OrderByDescending(m => m.Role) // First collaborators, then members
                    .ThenBy(m => m.JoinedAt)        // Then by join time (earliest first)
                    .FirstOrDefaultAsync();

                if (nextAdmin == null)
                {
                    // If no other members, delete the group
                    await DeleteGroupAsync(adminUserId, groupId);
                    return new AdminLeaveResult
                    {
                        Success = true,
                        Action = "delete",
                        GroupId = groupId
                    };
                }

                // Promote the next admin
                nextAdmin.Role = GroupRole.Admin;

                // Remove the current admin from the group
                _dbContext.GroupMembers.Remove(adminMember);

                await _dbContext.SaveChangesAsync();

                return new AdminLeaveResult
                {
                    Success = true,
                    Action = "leave",
                    GroupId = groupId,
                    NewAdminId = nextAdmin.UserId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing admin leave");
                throw;
            }
        }
        // Add these methods to GroupRepository class

        /// <summary>
        /// Checks if a user can send messages in a group (not muted or banned)
        /// </summary>
        public async Task<bool> CanSendMessagesAsync(Guid groupId, string userId)
        {
            // Check if user is muted
            var moderation = await _dbContext.Set<GroupMemberModeration>()
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (moderation == null)
                return true;

            // Check if banned
            if (moderation.IsBanned)
                return false;

            // Check if muted and mute hasn't expired
            if (moderation.IsMuted && moderation.MutedUntil.HasValue && moderation.MutedUntil.Value > DateTime.UtcNow)
                return false;

            // If mute has expired, update the status
            if (moderation.IsMuted && moderation.MutedUntil.HasValue && moderation.MutedUntil.Value <= DateTime.UtcNow)
            {
                moderation.IsMuted = false;
                moderation.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }

        /// <summary>
        /// Checks if a message has been deleted by a moderator
        /// </summary>
        public async Task<bool> IsMessageDeletedAsync(Guid messageId)
        {
            return await _dbContext.Set<GroupMessageModeration>()
                .AnyAsync(m => m.GroupMessageId == messageId && m.IsDeleted);
        }

        /// <summary>
        /// Filters a query of group messages to exclude deleted messages
        /// </summary>
        private async Task<List<GroupMessage>> FilterDeletedMessagesAsync(IQueryable<GroupMessage> query)
        {
            var messages = await query.ToListAsync();
            var messageIds = messages.Select(m => m.MessageId).ToList();

            var deletedMessageIds = await _dbContext.Set<GroupMessageModeration>()
                .Where(m => messageIds.Contains(m.GroupMessageId) && m.IsDeleted)
                .Select(m => m.GroupMessageId)
                .ToListAsync();

            // Replace content of deleted messages
            foreach (var message in messages)
            {
                if (deletedMessageIds.Contains(message.MessageId))
                {
                    message.Content = "[Message deleted by moderator]";
                }
            }

            return messages;
        }
        /// <summary>
        /// Uploads and sets a new avatar for a group
        /// </summary>
        /// <param name="userId">The ID of the user making the request (must be group admin)</param>
        /// <param name="groupId">The ID of the group to update</param>
        /// <param name="file">The image file to upload</param>
        /// <param name="firebaseHelper">Firebase helper for uploading files</param>
        /// <returns>The avatar information including URL</returns>
        public async Task<AvatarDTO> UploadGroupAvatarAsync(string userId, Guid groupId, IFormFile file, FirebaseHelper firebaseHelper)
        {
            if (file is null || file.Length == 0)
            {
                throw new ArgumentException("No file uploaded");
            }

            // Verify file is an image
            if (!file.ContentType.StartsWith("image/"))
            {
                throw new ArgumentException("Only image files are allowed");
            }

            // Find group and verify permissions
            var group = await _dbContext.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group is null)
                throw new KeyNotFoundException($"Group with ID {groupId} not found");

            // Check if user is an admin of this group
            var member = group.Members.FirstOrDefault(m => m.UserId == userId);

            if (member is null)
                throw new UnauthorizedAccessException("User is not a member of this group");

            if (member.Role != GroupRole.Admin)
                throw new UnauthorizedAccessException("Only group administrators can update the group avatar");

            try
            {
                // Upload file to Firebase Storage
                var avatarUrl = await firebaseHelper.UploadGroupAvatarAsync(groupId, file);

                // Update group record in database
                group.ImageUrl = avatarUrl;
                await _dbContext.SaveChangesAsync();

                return new AvatarDTO { AvatarUrl = avatarUrl };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for group {GroupId}", groupId);
                throw; // Re-throw to be handled by controller
            }
        }

        /// <summary>
        /// Gets a group by its ID
        /// </summary>
        /// <param name="userId">The ID of the user making the request (must be group member)</param>
        /// <param name="groupId">The ID of the group to retrieve</param>
        /// <returns>The group information</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the group is not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not a member of the group</exception>
        public async Task<GroupDTO> GetGroupByIdAsync(string userId, Guid groupId)
        {
            var group = await _dbContext.Groups
                .Include(g => g.Members)
                .Include(g => g.Creator)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group is null)
                throw new KeyNotFoundException($"Group with ID {groupId} not found");

            // Check if user is a member of this group
            var isMember = group.Members.Any(m => m.UserId == userId);

            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of this group");

            // Map to DTO
            var groupDto = _mapper.Map<GroupDTO>(group);

            // Set additional properties
            groupDto.CreatorName = $"{group.Creator.FirstName} {group.Creator.LastName}".Trim();
            groupDto.MemberCount = group.Members.Count;
            groupDto.Role = group.Members.FirstOrDefault(m => m.UserId == userId)?.Role ?? GroupRole.Member;

            return groupDto;
        }
        /// <summary>
        /// Updates a group's avatar URL
        /// </summary>
        /// <param name="userId">The ID of the user making the request (must be group admin)</param>
        /// <param name="groupId">The ID of the group to update</param>
        /// <param name="avatarUrl">The new avatar URL, or null to reset to default</param>
        /// <returns>True if successful</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the group is not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authorized to update the group</exception>
        public async Task<bool> UpdateGroupAvatarAsync(string userId, Guid groupId, string? avatarUrl)
        {
            var group = await _dbContext.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group is null)
                throw new KeyNotFoundException($"Group with ID {groupId} not found");

            // Check if user is an admin of this group
            var member = group.Members.FirstOrDefault(m => m.UserId == userId);

            if (member is null)
                throw new UnauthorizedAccessException("User is not a member of this group");

            if (member.Role != GroupRole.Admin)
                throw new UnauthorizedAccessException("Only group administrators can update the group avatar");

            // Update the avatar URL
            group.ImageUrl = avatarUrl;

            await _dbContext.SaveChangesAsync();

            return true;
        }

    }
}
