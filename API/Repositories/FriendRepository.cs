using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class FriendRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FriendRepository> _logger;

        public FriendRepository(
            ApplicationDbContext dbContext,
            IMapper mapper,
            ILogger<FriendRepository> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> AddFriendAsync(string userId, string friendId)
        {
            try
            {
                _logger.LogInformation("Adding friend request from {UserId} to {FriendId}", userId, friendId);

                // Validate users exist
                var user = await _dbContext.Users.FindAsync(userId);
                var friend = await _dbContext.Users.FindAsync(friendId);

                if (user == null || friend == null)
                {
                    _logger.LogWarning("User or friend not found");
                    return false;
                }

                // Check if relationship already exists
                var existingRelationship = await _dbContext.UserFriends
                    .FirstOrDefaultAsync(uf =>
                        (uf.UserId == userId && uf.FriendId == friendId) ||
                        (uf.UserId == friendId && uf.FriendId == userId));

                if (existingRelationship != null)
                {
                    _logger.LogInformation("Friend relationship already exists");
                    return false;
                }

                // Create new friend request
                var friendRequest = new UserFriend
                {
                    UserId = userId,
                    FriendId = friendId,
                    IsAccepted = false,
                    RequestedAt = DateTime.UtcNow
                };

                _dbContext.UserFriends.Add(friendRequest);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding friend");
                throw;
            }
        }
        /// <summary>
        /// Gets profile information for a specific friend
        /// </summary>
        /// <param name="friendId">The ID of the friend</param>
        /// <returns>Profile information for the friend</returns>
        /// <example>
        /// <code>
        /// var friendProfile = await _friendRepository.GetFriendProfileAsync("friend-user-id");
        /// </code>
        /// </example>
        public async Task<FriendDTO?> GetFriendProfileAsync(string friendId)
        {
            try
            {
                // Find the user in the database
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == friendId);

                if (user is null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", friendId);
                    return null;
                }

                // Create display name from user properties
                string displayName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
                    ? $"{user.FirstName} {user.LastName}"
                    : user.UserName ?? string.Empty;

                // Map the user to a FriendDTO
                var friendProfile = new FriendDTO
                {
                    UserId = user.Id,
                    Username = user.UserName ?? string.Empty,
                    DisplayName = displayName,
                    AvatarUrl = user.AvatarUrl,
                    LastActive = null, // ApplicationUser doesn't have LastActive property
                    IsOnline = false, // Set this based on your online tracking logic
                    IsPendingRequest = false // No pending request for an existing friend
                };

                return friendProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving friend profile for user {UserId}", friendId);
                throw;
            }
        }
        public async Task<bool> AcceptFriendRequestAsync(string userId, string friendId)
        {
            try
            {
                _logger.LogInformation("Accepting friend request from {FriendId} to {UserId}", friendId, userId);

                // Find the pending friend request
                var friendRequest = await _dbContext.UserFriends
                    .FirstOrDefaultAsync(uf => uf.UserId == friendId && uf.FriendId == userId && !uf.IsAccepted);

                if (friendRequest == null)
                {
                    _logger.LogWarning("Friend request not found");
                    return false;
                }

                // Accept the request
                friendRequest.IsAccepted = true;
                friendRequest.AcceptedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request");
                throw;
            }
        }

        public async Task<bool> RejectFriendRequestAsync(string userId, string friendId)
        {
            try
            {
                _logger.LogInformation("Rejecting friend request from {FriendId} to {UserId}", friendId, userId);

                // Find the pending friend request
                var friendRequest = await _dbContext.UserFriends
                    .FirstOrDefaultAsync(uf => uf.UserId == friendId && uf.FriendId == userId && !uf.IsAccepted);

                if (friendRequest == null)
                {
                    _logger.LogWarning("Friend request not found");
                    return false;
                }

                // Remove the friend request
                _dbContext.UserFriends.Remove(friendRequest);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting friend request");
                throw;
            }
        }

        public async Task<bool> RemoveFriendAsync(string userId, string friendId)
        {
            try
            {
                _logger.LogInformation("Removing friend relationship between {UserId} and {FriendId}", userId, friendId);

                // Find all friend relationships between these users (in either direction)
                var friendRelationships = await _dbContext.UserFriends
                    .Where(uf => (uf.UserId == userId && uf.FriendId == friendId) ||
                                (uf.UserId == friendId && uf.FriendId == userId))
                    .ToListAsync();

                if (!friendRelationships.Any())
                {
                    _logger.LogWarning("Friend relationship not found");
                    return false;
                }

                // Remove all relationships
                _dbContext.UserFriends.RemoveRange(friendRelationships);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing friend");
                throw;
            }
        }

        public async Task<IEnumerable<FriendDTO>> GetUserFriendsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting friends for user {UserId}", userId);

                // Get all accepted friend relationships where user is either requester or receiver
                var friends = await _dbContext.UserFriends
                    .Include(uf => uf.User)
                    .Include(uf => uf.Friend)
                    .Where(uf => (uf.UserId == userId || uf.FriendId == userId) && uf.IsAccepted)
                    .ToListAsync();

                // Proper way to use AutoMapper with context items
                return friends.Select(f => _mapper.Map<FriendDTO>(f, opts =>
                {
                    opts.Items["CurrentUserId"] = userId;
                })).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user friends");
                throw;
            }
        }

        public async Task<IEnumerable<FriendRequestDTO>> GetPendingFriendRequestsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting pending friend requests for user {UserId}", userId);

                // Get all pending friend requests sent to this user
                var pendingRequests = await _dbContext.UserFriends
                    .Include(uf => uf.User)
                    .Where(uf => uf.FriendId == userId && !uf.IsAccepted)
                    .ToListAsync();

                // Use AutoMapper instead of manual mapping
                return pendingRequests.Select(pr => _mapper.Map<FriendRequestDTO>(pr)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending friend requests");
                throw;
            }
        }
        public async Task<bool> IsFriendAsync(string userId, string otherUserId)
        {
            try
            {
                var friendship = await _dbContext.UserFriends
                    .FirstOrDefaultAsync(uf =>
                        (uf.UserId == userId && uf.FriendId == otherUserId ||
                        uf.UserId == otherUserId && uf.FriendId == userId) &&
                        uf.IsAccepted);

                return friendship != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if users {UserId} and {OtherUserId} are friends", userId, otherUserId);
                throw;
            }
        }
    }
}
