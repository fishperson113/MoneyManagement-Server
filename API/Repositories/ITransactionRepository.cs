using API.Models.DTOs;

namespace API.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<TransactionDTO>> GetAllTransactionsAsync();
    Task<IEnumerable<TransactionDTO>> GetTransactionsByWalletIdAsync(Guid walletId);
    Task<TransactionDTO?> GetTransactionByIdAsync(Guid transactionId);
    Task<TransactionDTO> CreateTransactionAsync(CreateTransactionDTO model);
    Task<TransactionDTO> UpdateTransactionAsync(UpdateTransactionDTO model);
    Task<Guid> DeleteTransactionByIdAsync(Guid transactionId);
    Task<IEnumerable<TransactionDetailDTO>> GetTransactionsByDateRangeAsync(
        DateTime startDate, DateTime endDate, string? type = null,
        string? category = null, string? timeRange = null, string? dayOfWeek = null);

    Task<IEnumerable<CategoryBreakdownDTO>> GetCategoryBreakdownAsync(
        DateTime startDate, DateTime endDate);

    Task<CashFlowSummaryDTO> GetCashFlowSummaryAsync(
        DateTime startDate, DateTime endDate);

    Task<IEnumerable<TransactionDetailDTO>> SearchTransactionsAsync(
        DateTime startDate, DateTime endDate, string? type = null,
        string? category = null, string? amountRange = null, string? keywords = null,
        string? timeRange = null, string? dayOfWeek = null);

    Task<DailySummaryDTO> GetDailySummaryAsync(DateTime date);
    Task<WeeklySummaryDTO> GetWeeklySummaryAsync(DateTime weekStartDate);
    Task<MonthlySummaryDTO> GetMonthlySummaryAsync(DateTime yearMonth);
    Task<YearlySummaryDTO> GetYearlySummaryAsync(int year);
}
