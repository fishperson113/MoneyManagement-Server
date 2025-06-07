// API/Controllers/GroupsController.cs (new file)
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly GroupRepository _groupRepository;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(GroupRepository groupRepository, ILogger<GroupsController> logger)
        {
            _groupRepository = groupRepository;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new group chat
        /// </summary>
        /// <param name="dto">Group creation data including name and optional member list</param>
        /// <returns>The newly created group</returns>
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var group = await _groupRepository.CreateGroupAsync(userId, dto);
                return Ok(group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                return StatusCode(500, "Có lỗi xảy ra khi tạo nhóm.");
            }
        }

        /// <summary>
        /// Gets all groups the current user is a member of
        /// </summary>
        /// <returns>List of group information</returns>
        [HttpGet]
        public async Task<IActionResult> GetUserGroups()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var groups = await _groupRepository.GetUserGroupsAsync(userId);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user groups");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách nhóm.");
            }
        }

        /// <summary>
        /// Gets chat history for a specific group
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <returns>Group info and message history</returns>
        [HttpGet("{groupId}/messages")]
        public async Task<IActionResult> GetGroupChatHistory(Guid groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var chatHistory = await _groupRepository.GetGroupChatHistoryAsync(userId, groupId);
                return Ok(chatHistory);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bạn không phải là thành viên của nhóm này.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Không tìm thấy nhóm.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group chat history");
                return StatusCode(500, "Có lỗi xảy ra khi lấy lịch sử trò chuyện nhóm.");
            }
        }

        /// <summary>
        /// Marks all messages in a group as read
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <returns>Success status</returns>
        [HttpPost("{groupId}/read")]
        public async Task<IActionResult> MarkGroupMessagesAsRead(Guid groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var result = await _groupRepository.MarkGroupMessagesAsReadAsync(userId, groupId);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking group messages as read");
                return StatusCode(500, "Có lỗi xảy ra khi đánh dấu tin nhắn đã đọc.");
            }
        }

        /// <summary>
        /// Gets all members of a group
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <returns>List of group members</returns>
        [HttpGet("{groupId}/members")]
        public async Task<IActionResult> GetGroupMembers(Guid groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var members = await _groupRepository.GetGroupMembersAsync(userId, groupId);
                return Ok(members);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bạn không phải là thành viên của nhóm này.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group members");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách thành viên nhóm.");
            }
        }

        /// <summary>
        /// Adds a user to a group (admin only)
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <param name="userId">The ID of the user to add</param>
        /// <returns>Success status</returns>
        [HttpPost("{groupId}/members/{userId}")]
        public async Task<IActionResult> AddUserToGroup(Guid groupId, string userId)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (adminUserId == null)
                    return Unauthorized();

                var result = await _groupRepository.AddUserToGroupAsync(adminUserId, groupId, userId);
                return Ok(new { success = result });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Chỉ quản trị viên nhóm mới có thể thêm thành viên.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to group");
                return StatusCode(500, "Có lỗi xảy ra khi thêm người dùng vào nhóm.");
            }
        }

        /// <summary>
        /// Removes a user from a group (admin only, or self-removal)
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <param name="userId">The ID of the user to remove</param>
        /// <returns>Success status</returns>
        [HttpDelete("{groupId}/members/{userId}")]
        public async Task<IActionResult> RemoveUserFromGroup(Guid groupId, string userId)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (adminUserId == null)
                    return Unauthorized();

                var result = await _groupRepository.RemoveUserFromGroupAsync(adminUserId, groupId, userId);
                return Ok(new { success = result });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bạn không có quyền xóa thành viên khỏi nhóm này.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from group");
                return StatusCode(500, "Có lỗi xảy ra khi xóa người dùng khỏi nhóm.");
            }
        }

        /// <summary>
        /// Updates group information (admin only)
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <param name="dto">Group update data</param>
        /// <returns>Success status</returns>
        [HttpPut("{groupId}")]
        public async Task<IActionResult> UpdateGroup(Guid groupId, [FromBody] UpdateGroupDTO dto)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (adminUserId == null)
                    return Unauthorized();

                var result = await _groupRepository.UpdateGroupAsync(adminUserId, groupId, dto);
                return Ok(new { success = result });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Chỉ quản trị viên nhóm mới có thể cập nhật thông tin nhóm.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Không tìm thấy nhóm.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật thông tin nhóm.");
            }
        }

        /// <summary>
        /// Sends a message to a group
        /// </summary>
        /// <param name="dto">The message data</param>
        /// <returns>The sent message</returns>
        [HttpPost("messages")]
        public async Task<IActionResult> SendGroupMessage([FromBody] SendGroupMessageDTO dto)
        {
            try
            {
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (senderId == null)
                    return Unauthorized();

                var message = await _groupRepository.SendGroupMessageAsync(senderId, dto);
                return Ok(message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bạn không phải là thành viên của nhóm này.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending group message");
                return StatusCode(500, "Có lỗi xảy ra khi gửi tin nhắn nhóm.");
            }
        }

        /// <summary>
        /// Allows an admin to leave a group with option to delete
        /// </summary>
        [HttpPost("{groupId}/admin-leave")]
        public async Task<IActionResult> AdminLeaveGroup(Guid groupId, [FromQuery] bool deleteGroup = false)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var result = await _groupRepository.AdminLeaveGroupAsync(userId, groupId, deleteGroup);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing admin leave");
                return StatusCode(500, "Có lỗi xảy ra khi rời khỏi nhóm.");
            }
        }

        /// <summary>
        /// Assigns collaborator role to a group member
        /// </summary>
        [HttpPost("{groupId}/members/{userId}/collaborator")]
        public async Task<IActionResult> AssignCollaboratorRole(Guid groupId, string userId)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (adminUserId == null)
                    return Unauthorized();

                var result = await _groupRepository.AssignCollaboratorRoleAsync(adminUserId, groupId, userId);
                return Ok(new { success = result });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Chỉ quản trị viên nhóm mới có thể chỉ định người cộng tác.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Không tìm thấy người dùng trong nhóm.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning collaborator role");
                return StatusCode(500, "Có lỗi xảy ra khi chỉ định quyền người cộng tác.");
            }
        }

        /// <summary>
        /// Allows a user to leave a group
        /// </summary>
        [HttpPost("{groupId}/leave")]
        public async Task<IActionResult> LeaveGroup(Guid groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                // Check if user is admin
                var members = await _groupRepository.GetGroupMembersAsync(userId, groupId);
                var currentMember = members.FirstOrDefault(m => m.UserId == userId);

                if (currentMember == null)
                    return NotFound("Group not found or user is not a member");

                // If admin, redirect to the admin leave endpoint
                if (currentMember.Role == GroupRole.Admin)
                    return BadRequest("Admins must use the admin-leave endpoint to leave groups");

                // Regular leave for non-admin members
                var result = await _groupRepository.RemoveUserFromGroupAsync(userId, groupId, userId);

                if (result)
                {
                    _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
                    return Ok(new { success = true });
                }
                else
                {
                    return NotFound("Group not found or user is not a member");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group");
                return StatusCode(500, "Có lỗi xảy ra khi rời khỏi nhóm.");
            }
        }

        /// <summary>
        /// Gets detailed profile information for a specific group member
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <param name="memberId">The ID of the member</param>
        /// <returns>The member's detailed profile information</returns>
        /// <response code="200">Returns the member profile</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not a member of the group</response>
        /// <response code="404">If the member is not found in the group</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("{groupId}/members/{memberId}/profile")]
        public async Task<IActionResult> GetGroupMemberProfile(Guid groupId, string memberId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                // First verify the requesting user is in the group
                try
                {
                    var members = await _groupRepository.GetGroupMembersAsync(userId, groupId);
                    var targetMember = members.FirstOrDefault(m => m.UserId == memberId);

                    if (targetMember == null)
                        return NotFound("Thành viên không tồn tại trong nhóm.");

                    // Get more detailed profile info
                    // We already have basic info in targetMember, but you might want to
                    // add a method to fetch additional details if needed
                    var memberProfile = new GroupMemberDTO
                    {
                        UserId = targetMember.UserId,
                        DisplayName = targetMember.DisplayName,
                        AvatarUrl = targetMember.AvatarUrl,
                        Role = targetMember.Role,
                    };

                    return Ok(memberProfile);
                }
                catch (UnauthorizedAccessException)
                {
                    return Forbid("Bạn không phải là thành viên của nhóm này.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving group member profile");
                return StatusCode(500, "Có lỗi xảy ra khi lấy thông tin thành viên nhóm.");
            }
        }
    }
}