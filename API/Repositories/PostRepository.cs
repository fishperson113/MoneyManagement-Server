using API.Config;
using API.Data;
using API.Helpers;
using API.Models.DTOs;
using API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API.Repositories
{
    public class PostRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PostRepository> _logger;
        private readonly FriendRepository _friendRepository;
        private readonly FirebaseHelper _firebaseHelper;
        public PostRepository(
            ApplicationDbContext dbContext,
            ILogger<PostRepository> logger,
            FriendRepository friendRepository,
            FirebaseHelper firebaseHelper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _friendRepository = friendRepository;
            _firebaseHelper = firebaseHelper;
        }

        public async Task<Post> CreatePostAsync(string userId, CreatePostDTO createPostDTO)
        {
            // Find user
            var user = await _dbContext.Users.FindAsync(userId);
            if (user is null)
            {
                throw new KeyNotFoundException("User not found");
            }

            // Validate group targeting if applicable
            if (createPostDTO.TargetType == PostTargetType.Groups && 
                createPostDTO.TargetGroupIds != null && 
                createPostDTO.TargetGroupIds.Any())
            {
                await ValidateUserGroupMembership(userId, createPostDTO.TargetGroupIds);
            }

            var post = new Post
            {
                PostId = Guid.NewGuid(),
                Content = createPostDTO.Content,
                CreatedAt = DateTime.UtcNow,
                AuthorId = userId,
                MediaUrl = createPostDTO.MediaFile,
                MediaType = createPostDTO.MediaType,
                TargetType = createPostDTO.TargetType,
                TargetGroupIds = createPostDTO.TargetGroupIds != null && createPostDTO.TargetGroupIds.Any() 
                    ? JsonSerializer.Serialize(createPostDTO.TargetGroupIds) 
                    : null
            };

            _dbContext.Posts.Add(post);
            await _dbContext.SaveChangesAsync();

            return post;
        }
        public async Task<bool> DeletePostAsync(string userId, Guid postId)
        {
            try
            {
                var post = await _dbContext.Posts
                    .Include(p => p.Comments)
                    .Include(p => p.Likes)
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null)
                {
                    return false;
                }

                // Only the author can delete their post
                if (post.AuthorId != userId)
                {
                    return false;
                }

                // Delete media from Firebase if exists
                if (!string.IsNullOrEmpty(post.MediaUrl))
                {
                    try
                    {
                        await _firebaseHelper.DeleteFileAsync(post.MediaUrl);
                        _logger.LogInformation("Deleted media for post {PostId} from Firebase", postId);
                    }
                    catch (Exception ex)
                    {
                        // Log but continue - we still want to delete the post even if media deletion fails
                        _logger.LogWarning(ex, "Failed to delete media for post {PostId} from Firebase", postId);
                    }
                }

                // Remove all comments and likes
                _dbContext.PostComments.RemoveRange(post.Comments);
                _dbContext.PostLikes.RemoveRange(post.Likes);

                // Remove the post
                _dbContext.Posts.Remove(post);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId} for user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<PostDetailDTO?> GetPostByIdAsync(string currentUserId, Guid postId)
        {
            try
            {
                // Get post with likes and comments
                var post = await _dbContext.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Likes)
                        .ThenInclude(l => l.User)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.Author)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.Replies.OrderBy(r => r.CreatedAt))
                            .ThenInclude(r => r.Author)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.Replies)
                            .ThenInclude(r => r.Replies.OrderBy(nr => nr.CreatedAt))
                                .ThenInclude(nr => nr.Author)
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null)
                {
                    return null;
                }

                // Check if current user can view this post based on targeting
                if (!await CanUserViewPost(currentUserId, post))
                {
                    return null; // Not authorized to view this post
                }

                // Helper function to recursively map nested replies
                List<PostCommentReplyDTO> MapReplies(IEnumerable<PostCommentReply> replies, string commentAuthorName, Dictionary<Guid, string> replyAuthorNames)
                {
                    return replies.Select(r => new PostCommentReplyDTO
                    {
                        ReplyId = r.ReplyId,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        AuthorId = r.AuthorId,
                        AuthorName = $"{r.Author.FirstName} {r.Author.LastName}".Trim(),
                        AuthorAvatarUrl = r.Author.AvatarUrl,
                        CommentId = r.CommentId,
                        ParentReplyId = r.ParentReplyId,
                        ParentReplyName = r.ParentReplyId.HasValue 
                            ? replyAuthorNames.GetValueOrDefault(r.ParentReplyId.Value)
                            : commentAuthorName,
                        Replies = MapReplies(r.Replies.OrderBy(nr => nr.CreatedAt), commentAuthorName, replyAuthorNames)
                    }).ToList();
                }

                // Convert to DTO
                return new PostDetailDTO
                {
                    PostId = post.PostId,
                    Content = post.Content,
                    CreatedAt = post.CreatedAt,
                    AuthorId = post.AuthorId,
                    AuthorName = $"{post.Author.FirstName} {post.Author.LastName}".Trim(),
                    AuthorAvatarUrl = post.Author.AvatarUrl,
                    LikesCount = post.Likes.Count,
                    CommentsCount = post.Comments.Count,
                    IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
                    MediaUrl = post.MediaUrl,
                    MediaType = post.MediaType,
                    TargetType = post.TargetType,
                    TargetGroupIds = !string.IsNullOrEmpty(post.TargetGroupIds)
                        ? JsonSerializer.Deserialize<List<Guid>>(post.TargetGroupIds)
                        : null,
                    // Map comments
                    Comments = post.Comments
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => 
                        {
                            // Create a dictionary of reply author names for this comment
                            var replyAuthorNames = c.Replies.ToDictionary(
                                r => r.ReplyId,
                                r => $"{r.Author.FirstName} {r.Author.LastName}".Trim()
                            );

                            return new PostCommentDTO
                            {
                                CommentId = c.CommentId,
                                Content = c.Content,
                                CreatedAt = c.CreatedAt,
                                AuthorId = c.AuthorId,
                                AuthorName = $"{c.Author.FirstName} {c.Author.LastName}".Trim(),
                                AuthorAvatarUrl = c.Author.AvatarUrl,
                                // Map top-level replies (only those without a parent)
                                Replies = MapReplies(
                                    c.Replies.Where(r => !r.ParentReplyId.HasValue).OrderBy(r => r.CreatedAt),
                                    $"{c.Author.FirstName} {c.Author.LastName}".Trim(),
                                    replyAuthorNames
                                )
                            };
                        })
                        .ToList(),

                    // Map likes
                    Likes = post.Likes
                        .Select(l => new PostLikeDTO
                        {
                            LikeId = l.LikeId,
                            UserId = l.UserId,
                            UserName = $"{l.User.FirstName} {l.User.LastName}".Trim(),
                            CreatedAt = l.CreatedAt
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post {PostId} for user {UserId}", postId, currentUserId);
                throw;
            }
        }
        public async Task<NewsFeedDTO> GetNewsFeedAsync(string userId, int page = 1, int pageSize = 10)
        {
            try
            {
                // Get user's friend IDs
                var friends = await _friendRepository.GetUserFriendsAsync(userId);
                var friendIds = friends.Select(f => f.UserId).ToList();

                // Get user's group memberships
                var userGroupIds = await _dbContext.GroupMembers
                    .Where(gm => gm.UserId == userId)
                    .Select(gm => gm.GroupId)
                    .ToListAsync();

                // Build the query with targeting logic
                var query = _dbContext.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .Where(p => 
                        // Private posts: only author can see
                        (p.TargetType == PostTargetType.Private && p.AuthorId == userId) ||
                        
                        // Friends posts: author's friends + author can see
                        (p.TargetType == PostTargetType.Friends && 
                         (friendIds.Contains(p.AuthorId) || p.AuthorId == userId)) ||
                        
                        // Global posts: author's friends + all group members where author is also a member
                        (p.TargetType == PostTargetType.Global && 
                         (friendIds.Contains(p.AuthorId) || p.AuthorId == userId ||
                          (_dbContext.GroupMembers.Any(gm1 => gm1.UserId == p.AuthorId && userGroupIds.Contains(gm1.GroupId))))) ||
                        
                        // Group-targeted posts: only members of targeted groups can see
                        (p.TargetType == PostTargetType.Groups && 
                         p.TargetGroupIds != null &&
                         userGroupIds.Any(ugId => p.TargetGroupIds.Contains(ugId.ToString())))
                    )
                    .OrderByDescending(p => p.CreatedAt);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var posts = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize + 1) // Take one extra to check if there are more posts
                    .ToListAsync();

                // Check if there are more posts
                bool hasMorePosts = posts.Count > pageSize;
                if (hasMorePosts)
                {
                    posts = posts.Take(pageSize).ToList();
                }

                // Convert to DTOs
                var postDTOs = posts.Select(p => new PostDTO
                {
                    PostId = p.PostId,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    AuthorId = p.AuthorId,
                    AuthorName = $"{p.Author.FirstName} {p.Author.LastName}".Trim(),
                    AuthorAvatarUrl = p.Author.AvatarUrl,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                    IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId),
                    MediaUrl = p.MediaUrl,
                    MediaType = p.MediaType,
                    TargetType = p.TargetType,
                    TargetGroupIds = !string.IsNullOrEmpty(p.TargetGroupIds) 
                        ? JsonSerializer.Deserialize<List<Guid>>(p.TargetGroupIds) 
                        : null
                }).ToList();

                return new NewsFeedDTO
                {
                    Posts = postDTOs,
                    TotalCount = totalCount,
                    HasMorePosts = hasMorePosts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting news feed for user {UserId}", userId);
                throw;
            }
        }

        public async Task<PostLike?> LikePostAsync(string userId, Guid postId)
        {
            try
            {
                // Check if post exists and user can see it
                var post = await _dbContext.Posts.FindAsync(postId);

                if (post == null)
                {
                    return null;
                }

                // Check if user can view this post (author or friend)
                if (post.AuthorId != userId)
                {
                    var isFriend = await _friendRepository.IsFriendAsync(userId, post.AuthorId);
                    if (!isFriend)
                    {
                        return null; // Not authorized to like this post
                    }
                }

                // Check if already liked
                var existingLike = await _dbContext.PostLikes
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                if (existingLike != null)
                {
                    return existingLike; // Already liked
                }

                // Create new like
                var like = new PostLike
                {
                    LikeId = Guid.NewGuid(),
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.PostLikes.AddAsync(like);
                await _dbContext.SaveChangesAsync();

                return like;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking post {PostId} for user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<bool> UnlikePostAsync(string userId, Guid postId)
        {
            try
            {
                var like = await _dbContext.PostLikes
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                if (like == null)
                {
                    return false; // Not liked
                }

                _dbContext.PostLikes.Remove(like);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unliking post {PostId} for user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<PostComment?> AddCommentAsync(string userId, CreateCommentDTO commentDTO)
        {
            try
            {
                // Check if post exists and user can see it
                var post = await _dbContext.Posts.FindAsync(commentDTO.PostId);

                if (post == null)
                {
                    return null;
                }

                // Check if user can view this post (author or friend)
                if (post.AuthorId != userId)
                {
                    var isFriend = await _friendRepository.IsFriendAsync(userId, post.AuthorId);
                    if (!isFriend)
                    {
                        return null; // Not authorized to comment on this post
                    }
                }

                // Create new comment
                var comment = new PostComment
                {
                    CommentId = Guid.NewGuid(),
                    Content = commentDTO.Content,
                    PostId = commentDTO.PostId,
                    AuthorId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.PostComments.AddAsync(comment);
                await _dbContext.SaveChangesAsync();

                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to post {PostId} for user {UserId}", commentDTO.PostId, userId);
                throw;
            }
        }



        public async Task<bool> DeleteCommentAsync(string userId, Guid commentId)
        {
            try
            {
                var comment = await _dbContext.PostComments
                    .Include(c => c.Post)
                    .FirstOrDefaultAsync(c => c.CommentId == commentId);

                if (comment == null)
                {
                    return false;
                }

                // Only the comment author or post author can delete a comment
                if (comment.AuthorId != userId && comment.Post.AuthorId != userId)
                {
                    return false;
                }

                _dbContext.PostComments.Remove(comment);
                await _dbContext.SaveChangesAsync();

                return true;            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId} for user {UserId}", commentId, userId);
                throw;
            }
        }

        public async Task<bool> UpdatePostTargetAsync(string userId, Guid postId, UpdatePostTargetDTO updateTargetDTO)
        {
            try
            {
                var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
                if (post == null || post.AuthorId != userId)
                {
                    return false; // Post not found or user is not the author
                }

                // Validate group targeting if applicable
                if (updateTargetDTO.TargetType == PostTargetType.Groups && 
                    updateTargetDTO.TargetGroupIds != null && 
                    updateTargetDTO.TargetGroupIds.Any())
                {
                    await ValidateUserGroupMembership(userId, updateTargetDTO.TargetGroupIds);
                }

                // Update the post
                post.TargetType = updateTargetDTO.TargetType;
                post.TargetGroupIds = updateTargetDTO.TargetGroupIds != null && updateTargetDTO.TargetGroupIds.Any() 
                    ? JsonSerializer.Serialize(updateTargetDTO.TargetGroupIds) 
                    : null;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post target for post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }

        private async Task ValidateUserGroupMembership(string userId, List<Guid> groupIds)
        {
            var userGroupIds = await _dbContext.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            var invalidGroupIds = groupIds.Where(gid => !userGroupIds.Contains(gid)).ToList();
            if (invalidGroupIds.Any())
            {
                throw new UnauthorizedAccessException($"User is not a member of groups: {string.Join(", ", invalidGroupIds)}");
            }
        }

        private async Task<bool> CanUserViewPost(string currentUserId, Post post)
        {
            switch (post.TargetType)
            {
                case PostTargetType.Private:
                    return post.AuthorId == currentUserId;

                case PostTargetType.Friends:
                    if (post.AuthorId == currentUserId)
                        return true;
                    return await _friendRepository.IsFriendAsync(currentUserId, post.AuthorId);

                case PostTargetType.Global:
                    if (post.AuthorId == currentUserId)
                        return true;
                    
                    // Check if they are friends
                    if (await _friendRepository.IsFriendAsync(currentUserId, post.AuthorId))
                        return true;
                    
                    // Check if they share any groups
                    var currentUserGroups = await _dbContext.GroupMembers
                        .Where(gm => gm.UserId == currentUserId)
                        .Select(gm => gm.GroupId)
                        .ToListAsync();
                    
                    var authorGroups = await _dbContext.GroupMembers
                        .Where(gm => gm.UserId == post.AuthorId)
                        .Select(gm => gm.GroupId)
                        .ToListAsync();
                    
                    return currentUserGroups.Any(cug => authorGroups.Contains(cug));

                case PostTargetType.Groups:
                    if (string.IsNullOrEmpty(post.TargetGroupIds))
                        return false;
                    
                    var targetGroupIds = JsonSerializer.Deserialize<List<Guid>>(post.TargetGroupIds);
                    if (targetGroupIds == null || !targetGroupIds.Any())
                        return false;
                    
                    var userGroupIds = await _dbContext.GroupMembers
                        .Where(gm => gm.UserId == currentUserId)
                        .Select(gm => gm.GroupId)
                        .ToListAsync();
                    
                    return targetGroupIds.Any(tgId => userGroupIds.Contains(tgId));

                default:
                    return false;
            }
        }
        public async Task<PostCommentReply?> AddCommentReplyAsync(string userId, CreateCommentReplyDTO replyDTO)
        {
            try
            {
                // Check if comment exists
                var comment = await _dbContext.PostComments
                    .Include(c => c.Post)
                    .FirstOrDefaultAsync(c => c.CommentId == replyDTO.CommentId);

                if (comment == null)
                {
                    return null;
                }

                // Check if user can view this post (author or friend)
                if (comment.Post.AuthorId != userId)
                {
                    var isFriend = await _friendRepository.IsFriendAsync(userId, comment.Post.AuthorId);
                    if (!isFriend)
                    {
                        return null; // Not authorized to reply to this comment
                    }
                }

                // If replying to another reply, validate that it exists and belongs to the same comment
                if (replyDTO.ParentReplyId.HasValue)
                {
                    var parentReply = await _dbContext.PostCommentReplies
                        .FirstOrDefaultAsync(r => r.ReplyId == replyDTO.ParentReplyId.Value && r.CommentId == replyDTO.CommentId);

                    if (parentReply == null)
                    {
                        return null; // Parent reply not found or not associated with this comment
                    }
                }

                // Create new reply
                var reply = new PostCommentReply
                {
                    ReplyId = Guid.NewGuid(),
                    Content = replyDTO.Content,
                    CommentId = replyDTO.CommentId,
                    AuthorId = userId,
                    CreatedAt = DateTime.UtcNow,
                    ParentReplyId = replyDTO.ParentReplyId
                };

                await _dbContext.PostCommentReplies.AddAsync(reply);
                await _dbContext.SaveChangesAsync();

                return reply;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reply to comment {CommentId} for user {UserId}", replyDTO.CommentId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteCommentReplyAsync(string userId, Guid replyId)
        {
            try
            {
                var reply = await _dbContext.PostCommentReplies
                    .Include(r => r.Comment)
                        .ThenInclude(c => c.Post)
                    .FirstOrDefaultAsync(r => r.ReplyId == replyId);

                if (reply == null)
                {
                    return false;
                }

                // Only the reply author, comment author, or post author can delete a reply
                if (reply.AuthorId != userId &&
                    reply.Comment.AuthorId != userId &&
                    reply.Comment.Post.AuthorId != userId)
                {
                    return false;
                }

                _dbContext.PostCommentReplies.Remove(reply);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reply {ReplyId} for user {UserId}", replyId, userId);
                throw;
            }
        }
        public async Task<PostComment?> GetCommentByIdAsync(Guid commentId)
        {
            return await _dbContext.PostComments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
        }

        public async Task<(string? Name, string? AvatarUrl)> GetUserBasicInfoAsync(string userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                return (null, null);

            return ($"{user.FirstName} {user.LastName}".Trim(), user.AvatarUrl);
        }
    }
}