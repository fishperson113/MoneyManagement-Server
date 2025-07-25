﻿using API.Helpers;
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NewsFeedController : ControllerBase
    {
        private readonly PostRepository _postRepository;
        private readonly ILogger<NewsFeedController> _logger;

        public NewsFeedController(
            PostRepository postRepository,
            ILogger<NewsFeedController> logger)
        {
            _postRepository = postRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<NewsFeedDTO>> GetNewsFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var newsFeed = await _postRepository.GetNewsFeedAsync(userId, page, pageSize);
                return Ok(newsFeed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving news feed");
                return StatusCode(500, "An error occurred while retrieving the news feed");
            }
        }

        [HttpGet("{postId}")]
        public async Task<ActionResult<PostDetailDTO>> GetPost(Guid postId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var post = await _postRepository.GetPostByIdAsync(userId, postId);
                if (post == null)
                {
                    return NotFound("Post not found or you don't have permission to view it");
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving post {PostId}", postId);
                return StatusCode(500, "An error occurred while retrieving the post");
            }
        }        /// <summary>
        /// Creates a new post with optional media attachment and targeting options
        /// </summary>
        /// <param name="content">Text content of the post</param>
        /// <param name="file">Optional media file to attach to the post</param>
        /// <param name="firebaseHelper">Firebase helper service for file uploads</param>
        /// <param name="category">Category for file organization</param>
        /// <param name="targetType">Post visibility type (Friends, Private, Global, Groups)</param>
        /// <param name="targetGroupIds">Comma-separated list of group IDs when targetType is Groups</param>
        /// <returns>The created post with full details</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<PostDTO>> CreatePost(
            [FromQuery] string content,
            IFormFile? file,
            [FromServices] FirebaseHelper firebaseHelper,
            [FromQuery] string category = "general",
            [FromQuery] PostTargetType targetType = PostTargetType.Friends,
            [FromQuery] string? targetGroupIds = null)        {
            // Check if there is at least content (file is now optional)
            if (string.IsNullOrWhiteSpace(content) && (file is null || file.Length == 0))
            {
                return BadRequest("Post must contain either text content or a media file");
            }

            // Get current user ID from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Parse target group IDs if provided
            List<Guid>? groupIds = null;
            if (targetType == PostTargetType.Groups && !string.IsNullOrWhiteSpace(targetGroupIds))
            {
                try
                {
                    groupIds = targetGroupIds.Split(',')
                        .Select(id => Guid.Parse(id.Trim()))
                        .ToList();
                }
                catch (FormatException)
                {
                    return BadRequest("Invalid group ID format");
                }
            }

            // Validate group targeting
            if (targetType == PostTargetType.Groups && (groupIds == null || !groupIds.Any()))
            {
                return BadRequest("Target groups must be specified when targeting groups");
            }

            try
            {
                string? fileUrl = null;
                string? mediaType = null;

                // Only process file if one was provided
                if (file is not null && file.Length > 0)
                {
                    // Determine folder based on media type
                    string folder;
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    // Images
                    if (extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp")
                    {
                        mediaType = "image";
                        folder = $"{category}/images/{userId}";
                    }
                    // Videos
                    else if (extension is ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm")
                    {
                        mediaType = "video";
                        folder = $"{category}/videos/{userId}";
                    }
                    // Audio
                    else if (extension is ".mp3" or ".wav" or ".ogg" or ".m4a" or ".flac")
                    {
                        mediaType = "audio";
                        folder = $"{category}/audio/{userId}";
                    }
                    // Documents
                    else if (extension is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt")
                    {
                        mediaType = "document";
                        folder = $"{category}/documents/{userId}";
                    }
                    // Other files
                    else
                    {
                        mediaType = "other";
                        folder = $"{category}/other/{userId}";
                    }

                    // Upload file
                    fileUrl = await firebaseHelper.UploadFileAsync(folder, file);
                }                // Create the post DTO with the content and optional media info
                var createPostDTO = new CreatePostDTO
                {
                    Content = content,
                    MediaFile = fileUrl,
                    MediaType = mediaType,
                    TargetType = targetType,
                    TargetGroupIds = groupIds
                };

                // Create the post
                var post = await _postRepository.CreatePostAsync(userId, createPostDTO);
                var postDetails = await _postRepository.GetPostByIdAsync(userId, post.PostId);

                return CreatedAtAction(nameof(GetPost), new { postId = post.PostId }, postDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return StatusCode(500, "An error occurred while creating the post");
            }
        }

        [HttpPost("{postId}/like")]
        public async Task<ActionResult> LikePost(Guid postId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var like = await _postRepository.LikePostAsync(userId, postId);
                if (like == null)
                {
                    return NotFound("Post not found or you don't have permission to like it");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking post {PostId}", postId);
                return StatusCode(500, "An error occurred while liking the post");
            }
        }

        [HttpDelete("{postId}/like")]
        public async Task<ActionResult> UnlikePost(Guid postId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _postRepository.UnlikePostAsync(userId, postId);
                if (!success)
                {
                    return NotFound("Post not found or not liked");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unliking post {PostId}", postId);
                return StatusCode(500, "An error occurred while unliking the post");
            }
        }

        [HttpPost("comment")]
        public async Task<ActionResult<PostCommentDTO>> AddComment([FromBody] CreateCommentDTO commentDTO)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var comment = await _postRepository.AddCommentAsync(userId, commentDTO);
                if (comment == null)
                {
                    return NotFound("Post not found or you don't have permission to comment on it");
                }

                // Get the updated post to return the formatted comment
                var post = await _postRepository.GetPostByIdAsync(userId, commentDTO.PostId);
                var commentDto = post.Comments.FirstOrDefault(c => c.CommentId == comment.CommentId);

                return Ok(commentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to post {PostId}", commentDTO.PostId);
                return StatusCode(500, "An error occurred while adding the comment");
            }
        }

        [HttpDelete("{postId}")]
        public async Task<ActionResult> DeletePost(Guid postId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _postRepository.DeletePostAsync(userId, postId);
                if (!success)
                {
                    return NotFound("Post not found or you don't have permission to delete it");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                return StatusCode(500, "An error occurred while deleting the post");
            }
        }        [HttpDelete("comment/{commentId}")]
        public async Task<ActionResult> DeleteComment(Guid commentId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _postRepository.DeleteCommentAsync(userId, commentId);
                if (!success)
                {
                    return NotFound("Comment not found or you don't have permission to delete it");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return StatusCode(500, "An error occurred while deleting the comment");
            }
        }

        [HttpPatch("{postId}/target")]
        public async Task<ActionResult> UpdatePostTarget(Guid postId, [FromBody] UpdatePostTargetDTO updateTargetDTO)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Validate group targeting
                if (updateTargetDTO.TargetType == PostTargetType.Groups && 
                    (updateTargetDTO.TargetGroupIds == null || !updateTargetDTO.TargetGroupIds.Any()))
                {
                    return BadRequest("Target groups must be specified when targeting groups");
                }

                var success = await _postRepository.UpdatePostTargetAsync(userId, postId, updateTargetDTO);
                if (!success)
                {
                    return NotFound("Post not found or you don't have permission to update it");
                }

                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("You are not a member of one or more of the specified groups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post target for post {PostId}", postId);
                return StatusCode(500, "An error occurred while updating the post target");
            }
        }
        [HttpPost("comment/reply")]
        public async Task<ActionResult<PostCommentReplyDTO>> AddCommentReply([FromBody] CreateCommentReplyDTO replyDTO)
        {
            try
            {
                if (replyDTO.ParentReplyId == Guid.Empty)
                {
                    replyDTO.ParentReplyId = null;
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var reply = await _postRepository.AddCommentReplyAsync(userId, replyDTO);
                if (reply == null)
                {
                    return NotFound("Comment not found or you don't have permission to reply to it");
                }

                // Get the comment's post ID from the repository
                var comment = await _postRepository.GetCommentByIdAsync(replyDTO.CommentId);
                if (comment == null)
                {
                    return NotFound("Comment not found");
                }

                // Get the updated post to return the formatted reply
                var post = await _postRepository.GetPostByIdAsync(userId, comment.PostId);
                if (post == null)
                {
                    return NotFound("Post not found");
                }

                var commentDto = post.Comments.FirstOrDefault(c => c.CommentId == replyDTO.CommentId);
                PostCommentReplyDTO? replyDto = null;

                if (replyDTO.ParentReplyId.HasValue)
                {
                    // Define the recursive function first
                    Func<List<PostCommentReplyDTO>, Guid, PostCommentReplyDTO?> findReplyRecursive = null!;
                    findReplyRecursive = (replies, targetReplyId) =>
                    {
                        foreach (var r in replies)
                        {
                            if (r.ReplyId == targetReplyId) return r;
                            var found = findReplyRecursive(r.Replies, targetReplyId);
                            if (found != null) return found;
                        }
                        return null;
                    };

                    // Now use the function
                    var parentReply = findReplyRecursive(commentDto?.Replies ?? new List<PostCommentReplyDTO>(), replyDTO.ParentReplyId.Value);
                    replyDto = parentReply?.Replies.FirstOrDefault(r => r.ReplyId == reply.ReplyId);
                }
                else
                {
                    // Direct reply to comment
                    replyDto = commentDto?.Replies.FirstOrDefault(r => r.ReplyId == reply.ReplyId);
                }

                if (replyDto == null)
                {
                    var authorInfo = await _postRepository.GetUserBasicInfoAsync(userId);
                    var authorName = authorInfo.Name ?? string.Empty;
                    var authorAvatarUrl = authorInfo.AvatarUrl;

                    replyDto = new PostCommentReplyDTO
                    {
                        ReplyId = reply.ReplyId,
                        Content = reply.Content,
                        CreatedAt = reply.CreatedAt,
                        AuthorId = userId,
                        AuthorName = authorName,
                        AuthorAvatarUrl = authorAvatarUrl,
                        CommentId = reply.CommentId,
                        ParentReplyId = reply.ParentReplyId,
                        Replies = new List<PostCommentReplyDTO>()
                    };
                }

                return Ok(replyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reply to comment {CommentId} for user {UserId}", replyDTO.CommentId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, "An error occurred while adding the reply");
            }
        }

        [HttpDelete("comment/reply/{replyId}")]
        public async Task<ActionResult> DeleteCommentReply(Guid replyId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _postRepository.DeleteCommentReplyAsync(userId, replyId);
                if (!success)
                {
                    return NotFound("Reply not found or you don't have permission to delete it");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reply {ReplyId}", replyId);
                return StatusCode(500, "An error occurred while deleting the reply");
            }
        }
    }
}