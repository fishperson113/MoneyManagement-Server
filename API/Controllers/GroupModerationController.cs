using API.Helpers;
using API.Models.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// Controller for group moderation operations
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GroupModerationController : ControllerBase
{
    private readonly IGroupModerationService _moderationService;

    public GroupModerationController(IGroupModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               throw new UnauthorizedAccessException("User is not authenticated");
    }

    /// <summary>
    /// Mutes a user in a group
    /// </summary>
    [HttpPost("mute")]
    public async Task<IActionResult> MuteUser(MuteUserRequest request)
    {
        var moderatorId = GetUserId();
        var result = await _moderationService.MuteUserAsync(
            moderatorId,
            request.GroupId,
            request.TargetUserId,
            request.Reason,
            TimeSpan.FromMinutes(request.DurationInMinutes)
        );

        if (!result)
            return BadRequest("Failed to mute user. Check if you have the required permissions.");

        return Ok();
    }

    /// <summary>
    /// Unmutes a user in a group
    /// </summary>
    [HttpPost("unmute")]
    public async Task<IActionResult> UnmuteUser(GroupUserActionRequest request)
    {
        var moderatorId = GetUserId();
        var result = await _moderationService.UnmuteUserAsync(
            moderatorId,
            request.GroupId,
            request.TargetUserId
        );

        if (!result)
            return BadRequest("Failed to unmute user. Check if you have the required permissions.");

        return Ok();
    }

    /// <summary>
    /// Bans a user from a group
    /// </summary>
    [HttpPost("ban")]
    public async Task<IActionResult> BanUser(BanKickUserRequest request)
    {
        var moderatorId = GetUserId();
        var result = await _moderationService.BanUserAsync(
            moderatorId,
            request.GroupId,
            request.TargetUserId,
            request.Reason
        );

        if (!result)
            return BadRequest("Failed to ban user. Check if you have the required permissions.");

        return Ok();
    }

    /// <summary>
    /// Unbans a user from a group
    /// </summary>
    [HttpPost("unban")]
    public async Task<IActionResult> UnbanUser(GroupUserActionRequest request)
    {
        var moderatorId = GetUserId();
        var result = await _moderationService.UnbanUserAsync(
            moderatorId,
            request.GroupId,
            request.TargetUserId
        );

        if (!result)
            return BadRequest("Failed to unban user. Check if you have the required permissions.");

        return Ok();
    }

    /// <summary>
    /// Kicks a user from a group
    /// </summary>
    [HttpPost("kick")]
    public async Task<IActionResult> KickUser(BanKickUserRequest request)
    {
        var moderatorId = GetUserId();
        var result = await _moderationService.KickUserAsync(
            moderatorId,
            request.GroupId,
            request.TargetUserId,
            request.Reason
        );

        if (!result)
            return BadRequest("Failed to kick user. Check if you have the required permissions.");

        return Ok();
    }

    /// <summary>
    /// Deletes a message from a group
    /// </summary>
    [HttpPost("delete-message")]
    public async Task<IActionResult> DeleteMessage(DeleteMessageRequest request)
    {
        var moderatorId = GetUserId();
        var result = await _moderationService.DeleteMessageAsync(
            moderatorId,
            request.GroupId,
            request.MessageId,
            request.Reason
        );

        if (!result)
            return BadRequest("Failed to delete message. Check if you have the required permissions.");

        return Ok();
    }

    /// <summary>
    /// Grants moderator role to a group member
    /// </summary>
    [HttpPost("grant-mod-role")]
    public async Task<IActionResult> GrantModRole(GroupUserActionRequest request)
    {
        var adminId = GetUserId();
        var result = await _moderationService.GrantModRoleAsync(
            adminId,
            request.GroupId,
            request.TargetUserId
        );

        if (!result)
            return BadRequest("Failed to grant moderator role. Only admins can perform this action.");

        return Ok();
    }

    /// <summary>
    /// Revokes moderator role from a group member
    /// </summary>
    [HttpPost("revoke-mod-role")]
    public async Task<IActionResult> RevokeModRole(GroupUserActionRequest request)
    {
        var adminId = GetUserId();
        var result = await _moderationService.RevokeModRoleAsync(
            adminId,
            request.GroupId,
            request.TargetUserId
        );

        if (!result)
            return BadRequest("Failed to revoke moderator role. Only admins can perform this action.");

        return Ok();
    }

    /// <summary>
    /// Gets moderation logs for a group
    /// </summary>
    [HttpGet("logs/{groupId}")]
    public async Task<IActionResult> GetModerationLogs(Guid groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _moderationService.GetModerationLogsAsync(groupId, page, pageSize);
        return Ok(logs);
    }
}