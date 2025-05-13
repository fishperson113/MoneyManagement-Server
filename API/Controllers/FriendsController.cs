using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Models.DTOs;
using API.Repositories;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FriendsController : ControllerBase
    {
        private readonly FriendRepository _friendRepository;
        private readonly ILogger<FriendsController> _logger;

        public FriendsController(FriendRepository friendRepository, ILogger<FriendsController> logger)
        {
            _friendRepository = friendRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var friends = await _friendRepository.GetUserFriendsAsync(userId);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friends");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách bạn bè.");
            }
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetFriendRequests()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var requests = await _friendRepository.GetPendingFriendRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friend requests");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách yêu cầu kết bạn.");
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddFriend([FromBody] AddFriendDTO dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                if (userId == dto.FriendId)
                    return BadRequest("Không thể kết bạn với chính mình.");

                var success = await _friendRepository.AddFriendAsync(userId, dto.FriendId);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding friend");
                return StatusCode(500, "Có lỗi xảy ra khi gửi yêu cầu kết bạn.");
            }
        }

        [HttpPost("accept/{friendId}")]
        public async Task<IActionResult> AcceptFriendRequest(string friendId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var success = await _friendRepository.AcceptFriendRequestAsync(userId, friendId);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request");
                return StatusCode(500, "Có lỗi xảy ra khi chấp nhận lời mời kết bạn.");
            }
        }

        [HttpPost("reject/{friendId}")]
        public async Task<IActionResult> RejectFriendRequest(string friendId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var success = await _friendRepository.RejectFriendRequestAsync(userId, friendId);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting friend request");
                return StatusCode(500, "Có lỗi xảy ra khi từ chối lời mời kết bạn.");
            }
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var success = await _friendRepository.RemoveFriendAsync(userId, friendId);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing friend");
                return StatusCode(500, "Có lỗi xảy ra khi xóa bạn bè.");
            }
        }
    }
}
