using API.Config;
using API.Data;
using API.Helpers;
using API.Models.DTOs;
using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                string? mediaUrl = null;
                string? mediaType = null;

                // Handle media upload if file is provided
                if (createPostDTO.MediaFile != null)
                {
                    // Determine folder based on media type
                    string folder;
                    var extension = Path.GetExtension(createPostDTO.MediaFile.FileName).ToLowerInvariant();

                    if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif")
                    {
                        mediaType = "image";
                        folder = $"posts/images/{userId}";
                    }
                    else if (extension == ".mp4" || extension == ".mov" || extension == ".avi")
                    {
                        mediaType = "video";
                        folder = $"posts/videos/{userId}";
                    }
                    else if (extension == ".mp3" || extension == ".wav" || extension == ".ogg")
                    {
                        mediaType = "audio";
                        folder = $"posts/audio/{userId}";
                    }
                    else
                    {
                        mediaType = "file";
                        folder = $"posts/files/{userId}";
                    }

                    // Use the existing UploadFileAsync method
                    mediaUrl = await _firebaseHelper.UploadFileAsync(folder, createPostDTO.MediaFile);
                }

                var post = new Post
                {
                    PostId = Guid.NewGuid(),
                    Content = createPostDTO.Content,
                    AuthorId = userId,
                    CreatedAt = DateTime.UtcNow,
                    MediaUrl = mediaUrl,
                    MediaType = mediaType
                };

                await _dbContext.Posts.AddAsync(post);
                await _dbContext.SaveChangesAsync();

                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post for user {UserId}", userId);
                throw;
            }
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
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null)
                {
                    return null;
                }

                // Check if current user can view this post (author or friend)
                if (post.AuthorId != currentUserId)
                {
                    var isFriend = await _friendRepository.IsFriendAsync(currentUserId, post.AuthorId);
                    if (!isFriend)
                    {
                        return null; // Not authorized to view this post
                    }
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

                    // Map comments
                    Comments = post.Comments
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => new PostCommentDTO
                        {
                            CommentId = c.CommentId,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt,
                            AuthorId = c.AuthorId,
                            AuthorName = $"{c.Author.FirstName} {c.Author.LastName}".Trim(),
                            AuthorAvatarUrl = c.Author.AvatarUrl
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
                // Get all friends
                var friends = await _friendRepository.GetUserFriendsAsync(userId);
                var friendIds = friends.Select(f => f.UserId).ToList();

                // Include the user's own posts
                friendIds.Add(userId);

                // Get posts from all friends and the user, ordered by creation date
                var query = _dbContext.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .Where(p => friendIds.Contains(p.AuthorId))
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
                    IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId)
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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId} for user {UserId}", commentId, userId);
                throw;
            }
        }
    }
}