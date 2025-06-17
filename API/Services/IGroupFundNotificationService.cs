using API.Models.DTOs;

namespace API.Services;

/// <summary>
/// Service interface for handling GroupFund-related notifications via group chat messages
/// This integrates transaction notifications into the group chat history
/// </summary>
public interface IGroupFundNotificationService
{
    /// <summary>
    /// Creates and broadcasts a group message for a GroupFund transaction update
    /// This message will appear in the group chat history and notify all members
    /// </summary>
    /// <param name="notification">The transaction data to broadcast</param>
    /// <example>
    /// <code>
    /// var notification = new GroupFundUpdateNotificationDTO
    /// {
    ///     GroupFundID = groupFund.GroupFundID,
    ///     GroupID = groupFund.GroupID,
    ///     NewBalance = groupFund.Balance,
    ///     TransactionID = transaction.GroupTransactionID,
    ///     TransactionType = transaction.Type,
    ///     TransactionAmount = transaction.Amount,
    ///     UpdatedAt = DateTime.UtcNow,
    ///     UserId = currentUserId
    /// };
    /// await _notificationService.SendGroupTransactionMessageAsync(notification);
    /// </code>
    /// </example>
    Task SendGroupTransactionMessageAsync(GroupFundUpdateNotificationDTO notification);

    /// <summary>
    /// Formats a transaction update into a readable group message content
    /// </summary>
    /// <param name="notification">The transaction notification data</param>
    /// <returns>Formatted message content for the group chat</returns>
    string FormatTransactionMessage(GroupFundUpdateNotificationDTO notification);
}
