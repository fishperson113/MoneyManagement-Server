using API.Data;
using API.Hub;
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

/// <summary>
/// Service for handling GroupFund-related notifications via group chat messages
/// Follows the safe extension pattern: integrates with existing group messaging system
/// instead of creating a separate notification system
/// </summary>
public class GroupFundNotificationService : IGroupFundNotificationService
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    private readonly GroupRepository _groupRepository;
    private readonly ILogger<GroupFundNotificationService> _logger;

    public GroupFundNotificationService(
        IHubContext<ChatHub, IChatClient> hubContext,
        GroupRepository groupRepository,
        ILogger<GroupFundNotificationService> logger)
    {
        _hubContext = hubContext;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates and broadcasts a group message for a GroupFund transaction update
    /// This message will appear in the group chat history and notify all members
    /// </summary>
    /// <param name="notification">The transaction data to broadcast</param>
    public async Task SendGroupTransactionMessageAsync(GroupFundUpdateNotificationDTO notification)
    {
        try
        {
            _logger.LogInformation("Sending group transaction message for GroupID: {GroupID}, Transaction: {TransactionID}",
                notification.GroupID, notification.TransactionID);

            // Format the transaction as a readable group message
            var messageContent = FormatTransactionMessage(notification);

            // Create a system message DTO for the group chat
            var systemMessageDto = new SendGroupMessageDTO
            {
                GroupId = notification.GroupID,
                Content = messageContent
            };

            // Use the existing group repository to send the message as the transaction creator
            var groupMessage = await _groupRepository.SendGroupMessageAsync(notification.UserId, systemMessageDto);

            // Get all group members to notify
            var groupMembers = await _groupRepository.GetGroupMembersAsync(notification.UserId, notification.GroupID);

            // Broadcast the message to all online group members using existing SignalR infrastructure
            foreach (var member in groupMembers)
            {
                try
                {
                    // Use the existing ReceiveGroupMessage method - no new SignalR methods needed
                    await _hubContext.Clients.User(member.UserId).ReceiveGroupMessage(groupMessage);
                    _logger.LogDebug("Sent group transaction message to user: {UserId}", member.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send group transaction message to user: {UserId}", member.UserId);
                    // Continue with other members even if one fails
                }
            }

            _logger.LogInformation("Successfully sent group transaction message to {MemberCount} members of GroupID: {GroupID}",
                groupMembers.Count(), notification.GroupID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending group transaction message for GroupID: {GroupID}, TransactionID: {TransactionID}",
                notification.GroupID, notification.TransactionID);
            throw;
        }
    }

    /// <summary>
    /// Formats a transaction update into a readable group message content
    /// </summary>
    /// <param name="notification">The transaction notification data</param>
    /// <returns>Formatted message content for the group chat</returns>
    public string FormatTransactionMessage(GroupFundUpdateNotificationDTO notification)
    {
        var transactionTypeIcon = notification.TransactionType.ToLower() == "income" ? "💰" : "💸";
        var balanceFormatted = notification.NewBalance.ToString("C2");
        var amountFormatted = notification.TransactionAmount.ToString("C2");
        
        var message = $"{transactionTypeIcon} **Group Fund Update**\n" +
                     $"**{notification.TransactionType.ToUpper()}**: {amountFormatted}\n" +
                     $"**New Balance**: {balanceFormatted}";

        if (!string.IsNullOrEmpty(notification.TransactionDescription))
        {
            message += $"\n**Description**: {notification.TransactionDescription}";
        }

        message += $"\n\n_Transaction ID: {notification.TransactionID}_";

        return message;
    }
}
