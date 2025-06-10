using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using API.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

/// <summary>
/// Statistics service implementation for generating report data
/// </summary>
public class StatisticService : IStatisticService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<StatisticService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StatisticService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<StatisticService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Generates raw report data based on configuration
    /// </summary>
    /// <param name="reportInfo">Report configuration</param>
    /// <returns>Raw data object for the report</returns>
    public async Task<object> GenerateReportDataAsync(CreateReportDTO reportInfo)
    {
        try
        {
            _logger.LogInformation("Generating report data for type {Type} from {Start} to {End}",
                reportInfo.Type, reportInfo.StartDate, reportInfo.EndDate);

            var userId = GetCurrentUserId();
            var userWalletIds = await GetUserWalletIdsAsync(userId);
            var transactions = await GetTransactionsAsync(reportInfo, userWalletIds);

            // Factory Pattern: Create different data structures based on report type
            return reportInfo.Type?.ToLower() switch
            {
                "cash-flow" => await GenerateCashFlowDataAsync(transactions),
                "category-breakdown" => await GenerateCategoryBreakdownDataAsync(reportInfo.StartDate, reportInfo.EndDate, userId),
                "daily-summary" => await GenerateDailySummaryDataAsync(reportInfo.StartDate, reportInfo.EndDate, transactions),
                "weekly-summary" => await GenerateWeeklySummaryDataAsync(reportInfo.StartDate, transactions),
                "monthly-summary" => await GenerateMonthlySummaryDataAsync(reportInfo.StartDate, transactions),
                "yearly-summary" => await GenerateYearlySummaryDataAsync(reportInfo.StartDate.Year, transactions),
                _ => await GenerateTransactionReportDataAsync(transactions, reportInfo.StartDate, reportInfo.EndDate)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report data for type {Type}", reportInfo.Type);
            throw;
        }
    }

    #region Data Generation Methods

    /// <summary>
    /// Generates cash flow summary data
    /// </summary>
    private async Task<CashFlowSummaryDTO> GenerateCashFlowDataAsync(List<Transaction> transactions)
    {
        await Task.CompletedTask;
        
        var totalIncome = transactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpenses = Math.Abs(transactions.Where(t => t.Type == "expense").Sum(t => t.Amount));

        return new CashFlowSummaryDTO
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetCashFlow = totalIncome - totalExpenses
        };
    }

    /// <summary>
    /// Generates category breakdown data
    /// </summary>
    private async Task<IEnumerable<CategoryBreakdownDTO>> GenerateCategoryBreakdownDataAsync(
        DateTime startDate, DateTime endDate, string userId)
    {
        var userWalletIds = await GetUserWalletIdsAsync(userId);

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.TransactionDate >= startDate &&
                   t.TransactionDate <= endDate &&
                   userWalletIds.Contains(t.WalletID))
            .ToListAsync();

        var totalIncome = transactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpense = Math.Abs(transactions.Where(t => t.Type == "expense").Sum(t => t.Amount));

        return transactions
            .GroupBy(t => t.Category.Name)
            .Select(g => new CategoryBreakdownDTO
            {
                Category = g.Key,
                TotalIncome = g.Where(t => t.Type == "income").Sum(t => t.Amount),
                TotalExpense = Math.Abs(g.Where(t => t.Type == "expense").Sum(t => t.Amount)),
                IncomePercentage = totalIncome == 0 ? 0 : Math.Round(g.Where(t => t.Type == "income").Sum(t => t.Amount) / totalIncome * 100, 2),
                ExpensePercentage = totalExpense == 0 ? 0 : Math.Round(Math.Abs(g.Where(t => t.Type == "expense").Sum(t => t.Amount)) / totalExpense * 100, 2)
            })
            .OrderByDescending(c => c.TotalIncome + c.TotalExpense)
            .ToList();
    }

    /// <summary>
    /// Generates daily summary data
    /// </summary>
    private async Task<object> GenerateDailySummaryDataAsync(DateTime startDate, DateTime endDate, List<Transaction> transactions)
    {
        await Task.CompletedTask;

        var dailyData = transactions
            .GroupBy(t => t.TransactionDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Income = g.Where(t => t.Type == "income").Sum(t => t.Amount),
                Expenses = Math.Abs(g.Where(t => t.Type == "expense").Sum(t => t.Amount)),
                TransactionCount = g.Count(),
                NetFlow = g.Where(t => t.Type == "income").Sum(t => t.Amount) - 
                         Math.Abs(g.Where(t => t.Type == "expense").Sum(t => t.Amount))
            })
            .OrderBy(x => x.Date)
            .ToList();

        return new
        {
            StartDate = startDate,
            EndDate = endDate,
            DailyBreakdown = dailyData,
            TotalDays = dailyData.Count,
            TotalIncome = dailyData.Sum(d => d.Income),
            TotalExpenses = dailyData.Sum(d => d.Expenses),
            AverageDaily = dailyData.Count > 0 ? dailyData.Sum(d => d.NetFlow) / dailyData.Count : 0,
            Transactions = _mapper.Map<List<TransactionDetailDTO>>(transactions)
        };
    }

    /// <summary>
    /// Generates weekly summary data
    /// </summary>
    private async Task<object> GenerateWeeklySummaryDataAsync(DateTime startDate, List<Transaction> transactions)
    {
        await Task.CompletedTask;

        var weekStart = startDate.Date.AddDays(-(int)(startDate.DayOfWeek == DayOfWeek.Sunday ? 6 : startDate.DayOfWeek - DayOfWeek.Monday));
        var weekEnd = weekStart.AddDays(7);

        var weekTransactions = transactions
            .Where(t => t.TransactionDate >= weekStart && t.TransactionDate < weekEnd)
            .ToList();

        var totalIncome = weekTransactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpenses = Math.Abs(weekTransactions.Where(t => t.Type == "expense").Sum(t => t.Amount));

        return new
        {
            StartDate = weekStart,
            EndDate = weekEnd,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetCashFlow = totalIncome - totalExpenses,
            Transactions = _mapper.Map<List<TransactionDetailDTO>>(weekTransactions)
        };
    }

    /// <summary>
    /// Generates monthly summary data
    /// </summary>
    private async Task<object> GenerateMonthlySummaryDataAsync(DateTime startDate, List<Transaction> transactions)
    {
        await Task.CompletedTask;

        var monthStart = new DateTime(startDate.Year, startDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        var monthTransactions = transactions
            .Where(t => t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
            .ToList();

        var totalIncome = monthTransactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpenses = Math.Abs(monthTransactions.Where(t => t.Type == "expense").Sum(t => t.Amount));

        return new
        {
            Month = startDate.Month,
            Year = startDate.Year,
            MonthName = startDate.ToString("MMMM"),
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetCashFlow = totalIncome - totalExpenses,
            Transactions = _mapper.Map<List<TransactionDetailDTO>>(monthTransactions)
        };
    }

    /// <summary>
    /// Generates yearly summary data
    /// </summary>
    private async Task<object> GenerateYearlySummaryDataAsync(int year, List<Transaction> transactions)
    {
        await Task.CompletedTask;

        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31, 23, 59, 59, 999);

        var yearTransactions = transactions
            .Where(t => t.TransactionDate >= yearStart && t.TransactionDate <= yearEnd)
            .ToList();

        var totalIncome = yearTransactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpenses = Math.Abs(yearTransactions.Where(t => t.Type == "expense").Sum(t => t.Amount));

        return new
        {
            Year = year,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetCashFlow = totalIncome - totalExpenses,
            Transactions = _mapper.Map<List<TransactionDetailDTO>>(yearTransactions)
        };
    }

    /// <summary>
    /// Generates transaction report data (default)
    /// </summary>
    private async Task<object> GenerateTransactionReportDataAsync(List<Transaction> transactions, DateTime startDate, DateTime endDate)
    {
        await Task.CompletedTask;

        var transactionDTOs = _mapper.Map<List<TransactionDetailDTO>>(transactions);
        var totalIncome = transactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpenses = Math.Abs(transactions.Where(t => t.Type == "expense").Sum(t => t.Amount));

        return new
        {
            Transactions = transactionDTOs,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetCashFlow = totalIncome - totalExpenses,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets current user ID from HTTP context
    /// </summary>
    private string GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid");
        if (userIdClaim is null)
            throw new UnauthorizedAccessException("User not authenticated");
        
        return userIdClaim.Value;
    }

    /// <summary>
    /// Gets all wallet IDs for the current user
    /// </summary>
    private async Task<List<Guid>> GetUserWalletIdsAsync(string userId)
    {
        return await _context.Wallets
            .Where(w => w.UserId == userId)
            .Select(w => w.WalletID)
            .ToListAsync();
    }

    /// <summary>
    /// Gets transactions for the report based on configuration
    /// </summary>
    private async Task<List<Transaction>> GetTransactionsAsync(CreateReportDTO reportInfo, List<Guid> userWalletIds)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Wallet)
            .Where(t => t.TransactionDate >= reportInfo.StartDate &&
                   t.TransactionDate <= reportInfo.EndDate &&
                   userWalletIds.Contains(t.WalletID));

        return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
    }

    #endregion
}
