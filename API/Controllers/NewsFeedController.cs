using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        }

        [HttpPost]
        public async Task<ActionResult<PostDTO>> CreatePost([FromBody] CreatePostDTO createPostDTO)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

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
        }

        [HttpDelete("comment/{commentId}")]
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
    }
}