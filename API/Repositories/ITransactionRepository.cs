using API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Repositories
{
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

        Task<IEnumerable<AggregateStatisticsDTO>> GetAggregateStatisticsAsync(
            string period, DateTime startDate, DateTime endDate, string? type = null);

        Task<IEnumerable<CategoryBreakdownDTO>> GetCategoryBreakdownAsync(
            DateTime startDate, DateTime endDate, string? type = null);

        Task<CashFlowSummaryDTO> GetCashFlowSummaryAsync(
            DateTime startDate, DateTime endDate);

        Task<IEnumerable<TransactionDetailDTO>> SearchTransactionsAsync(
            DateTime startDate, DateTime endDate, string? type = null,
            string? category = null, string? amountRange = null, string? keywords = null,
            string? timeRange = null, string? dayOfWeek = null);

        Task<DailySummaryDTO> GetDailySummaryAsync(DateTime date);

        Task<ReportInfoDTO> GenerateReportAsync(
            DateTime startDate, DateTime endDate, string? type, string format,
            bool includeTime = false, bool includeDayMonth = false);

        Task<(string fileName, string contentType, byte[] fileBytes)> DownloadReportAsync(int reportId);

    }
}
