using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace API.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TransactionRepository(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<TransactionRepository> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User is not authenticated");
        }
        public async Task<TransactionDTO> CreateTransactionAsync(CreateTransactionDTO model)
        {
            try
            {
                _logger.LogInformation("Starting transaction creation.");

                var userId = GetCurrentUserId();

                // Verify that the wallet belongs to the current user
                var walletBelongsToUser = await _context.Wallets
                    .AnyAsync(w => w.WalletID == model.WalletID && w.UserId == userId);

                if (!walletBelongsToUser)
                {
                    _logger.LogWarning("Cannot create transaction for wallet {WalletID} as it doesn't belong to current user", model.WalletID);
                    throw new UnauthorizedAccessException($"Cannot create transaction for wallet {model.WalletID} as it doesn't belong to current user");
                }

                var transaction = _mapper.Map<Transaction>(model);
                transaction.TransactionID = Guid.NewGuid();

                // Set the Type based on the provided value or calculate it from Amount
                if (!string.IsNullOrEmpty(model.Type) &&
                    (model.Type.ToLower() == "income" || model.Type.ToLower() == "expense"))
                {
                    transaction.Type = model.Type.ToLower();
                }
                else
                {
                    transaction.Type = transaction.Amount < 0 ? "expense" : "income";
                }

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction created successfully with ID: {TransactionID}", transaction.TransactionID);
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating transaction.");
                throw;
            }
        }

        public async Task<TransactionDTO> UpdateTransactionAsync(UpdateTransactionDTO model)
        {
            try
            {
                _logger.LogInformation("Updating transaction with ID: {TransactionID}", model.TransactionID);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionID == model.TransactionID && userWalletIds.Contains(t.WalletID));

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found or doesn't belong to current user", model.TransactionID);
                    throw new KeyNotFoundException($"Transaction with ID {model.TransactionID} not found or access denied.");
                }

                // Verify that the target wallet also belongs to the user if it's being changed
                if (transaction.WalletID != model.WalletID && !userWalletIds.Contains(model.WalletID))
                {
                    _logger.LogWarning("Cannot move transaction to wallet {WalletID} as it doesn't belong to current user", model.WalletID);
                    throw new UnauthorizedAccessException($"Cannot move transaction to wallet {model.WalletID} as it doesn't belong to current user");
                }

                _mapper.Map(model, transaction);

                // Update the Type based on the provided value or calculate it from Amount
                if (!string.IsNullOrEmpty(model.Type) &&
                    (model.Type.ToLower() == "income" || model.Type.ToLower() == "expense"))
                {
                    transaction.Type = model.Type.ToLower();
                }
                else
                {
                    transaction.Type = transaction.Amount < 0 ? "expense" : "income";
                }

                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction with ID {TransactionID} updated successfully", transaction.TransactionID);
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating transaction.");
                throw;
            }
        }

        public async Task<Guid> DeleteTransactionByIdAsync(Guid transactionId)
        {
            try
            {
                _logger.LogInformation("Deleting transaction with ID: {TransactionID}", transactionId);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionID == transactionId && userWalletIds.Contains(t.WalletID));

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found or doesn't belong to current user", transactionId);
                    throw new KeyNotFoundException($"Transaction with ID {transactionId} not found or access denied.");
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction with ID {TransactionID} deleted successfully", transactionId);
                return transactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting transaction.");
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetAllTransactionsAsync()
        {
            _logger.LogInformation("Fetching all transactions for the current user.");

            try
            {
                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                // Get transactions only from those wallets
                var transactions = await _context.Transactions
                    .Where(t => userWalletIds.Contains(t.WalletID))
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} transactions for user {UserId}.",
                    transactions.Count, userId);

                return _mapper.Map<IEnumerable<TransactionDTO>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching transactions.");
                throw;
            }
        }

        public async Task<TransactionDTO?> GetTransactionByIdAsync(Guid transactionId)
        {
            _logger.LogInformation("Fetching transaction with ID: {TransactionID}", transactionId);

            try
            {
                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionID == transactionId && userWalletIds.Contains(t.WalletID));

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found or doesn't belong to current user.", transactionId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved transaction with ID: {TransactionID}", transactionId);
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching transaction with ID: {TransactionID}", transactionId);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetTransactionsByWalletIdAsync(Guid walletId)
        {
            _logger.LogInformation("Fetching transactions for wallet with ID: {WalletID}", walletId);

            try
            {
                var userId = GetCurrentUserId();

                // Verify that the wallet belongs to the current user
                var walletBelongsToUser = await _context.Wallets
                    .AnyAsync(w => w.WalletID == walletId && w.UserId == userId);

                if (!walletBelongsToUser)
                {
                    _logger.LogWarning("Wallet with ID {WalletID} not found or doesn't belong to current user", walletId);
                    throw new UnauthorizedAccessException($"Wallet with ID {walletId} not found or access denied");
                }

                var transactions = await _context.Transactions
                    .Where(t => t.WalletID == walletId)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} transactions for wallet with ID: {WalletID}",
                    transactions.Count, walletId);

                return _mapper.Map<IEnumerable<TransactionDTO>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching transactions for wallet with ID: {WalletID}", walletId);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDetailDTO>> GetTransactionsByDateRangeAsync(
            DateTime startDate, DateTime endDate, string? type = null,
            string? category = null, string? timeRange = null, string? dayOfWeek = null)
        {
            try
            {
                _logger.LogInformation("Fetching transactions for date range {StartDate} to {EndDate}", startDate, endDate);
                var userId = GetCurrentUserId();
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();
                var query = _context.Transactions
                     .Include(t => t.Category)
                     .Include(t => t.Wallet)
                     .Where(t => t.TransactionDate >= startDate &&
                            t.TransactionDate <= endDate &&
                            userWalletIds.Contains(t.WalletID));

                // Apply type filter (income/expense)
                if (!string.IsNullOrEmpty(type))
                {
                    bool isExpense = type.ToLower() == "expense";
                    query = query.Where(t => isExpense ? t.Amount < 0 : t.Amount > 0);
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(t => t.Category.Name.ToLower() == category.ToLower());
                }

                // Apply time range filter
                if (!string.IsNullOrEmpty(timeRange))
                {
                    var times = timeRange.Split('-');
                    if (times.Length == 2)
                    {
                        var startTime = TimeSpan.Parse(times[0]);
                        var endTime = TimeSpan.Parse(times[1]);

                        query = query.Where(t =>
                            t.TransactionDate.TimeOfDay >= startTime &&
                            t.TransactionDate.TimeOfDay <= endTime);
                    }
                }

                // Apply day of week filter
                if (!string.IsNullOrEmpty(dayOfWeek))
                {
                    if (Enum.TryParse<DayOfWeek>(dayOfWeek, true, out var day))
                    {
                        query = query.Where(t => t.TransactionDate.DayOfWeek == day);
                    }
                }

                var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync();

                return _mapper.Map<IEnumerable<TransactionDetailDTO>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions by date range");
                throw;
            }
        }

        public async Task<IEnumerable<CategoryBreakdownDTO>> GetCategoryBreakdownAsync(
            DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Generating category breakdown for period {Start} to {End}",
                    startDate, endDate);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           userWalletIds.Contains(t.WalletID))
                    .ToListAsync();

                // Split transactions into income and expenses
                var incomeTransactions = transactions.Where(t => t.Amount > 0).ToList();
                var expenseTransactions = transactions.Where(t => t.Amount < 0).ToList();

                // Calculate totals for income and expenses
                var totalIncome = incomeTransactions.Sum(t => t.Amount);
                var totalExpense = Math.Abs(expenseTransactions.Sum(t => t.Amount));

                // Group by category and calculate totals and percentages
                var result = transactions
                    .GroupBy(t => new { CategoryName = t.Category.Name, IsIncome = t.Amount > 0 })
                    .Select(g => new CategoryBreakdownDTO
                    {
                        Category = g.Key.CategoryName,
                        IsIncome = g.Key.IsIncome,
                        Total = g.Sum(t => Math.Abs(t.Amount)),
                        Percentage = g.Key.IsIncome
                            ? (totalIncome == 0 ? 0 : Math.Round(g.Sum(t => t.Amount) / totalIncome * 100, 2))
                            : (totalExpense == 0 ? 0 : Math.Round(Math.Abs(g.Sum(t => t.Amount)) / totalExpense * 100, 2)),
                        TotalIncome = totalIncome,
                        TotalExpense = totalExpense
                    })
                    .OrderByDescending(c => c.Total)
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating category breakdown");
                throw;
            }
        }

        public async Task<CashFlowSummaryDTO> GetCashFlowSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Generating cash flow summary for period {Start} to {End}", startDate, endDate);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var transactions = await _context.Transactions
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && userWalletIds.Contains(t.WalletID))
                    .ToListAsync();

                var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var totalExpenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));

                return new CashFlowSummaryDTO
                {
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    NetCashFlow = totalIncome - totalExpenses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cash flow summary");
                throw;
            }
        }


        public async Task<IEnumerable<TransactionDetailDTO>> SearchTransactionsAsync(
            DateTime startDate, DateTime endDate, string? type = null,
            string? category = null, string? amountRange = null, string? keywords = null,
            string? timeRange = null, string? dayOfWeek = null)
        {
            try
            {
                _logger.LogInformation("Searching transactions with complex filters");

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                endDate = endDate.Date.AddDays(1).AddMilliseconds(-1);

                var query = _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           userWalletIds.Contains(t.WalletID));

                // Apply type filter (income/expense)
                if (!string.IsNullOrEmpty(type))
                {
                    bool isExpense = type.ToLower() == "expense";
                    query = query.Where(t => isExpense ? t.Amount < 0 : t.Amount > 0);
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(t => t.Category.Name.ToLower() == category.ToLower());
                }

                // Apply amount range filter
                if (!string.IsNullOrEmpty(amountRange))
                {
                    var range = amountRange.Split('-');
                    if (range.Length == 2 &&
                        decimal.TryParse(range[0], out decimal minAmount) &&
                        decimal.TryParse(range[1], out decimal maxAmount))
                    {
                        // For expenses, we use absolute values for comparison
                        if (!string.IsNullOrEmpty(type) && type.ToLower() == "expense")
                        {
                            query = query.Where(t => Math.Abs(t.Amount) >= minAmount && Math.Abs(t.Amount) <= maxAmount);
                        }
                        else
                        {
                            query = query.Where(t => t.Amount >= minAmount && t.Amount <= maxAmount);
                        }
                    }
                }

                // Apply keyword search
                if (!string.IsNullOrEmpty(keywords))
                {
                    query = query.Where(t => t.Description != null && t.Description.Contains(keywords));
                }

                // Apply time range filter
                if (!string.IsNullOrEmpty(timeRange))
                {
                    var times = timeRange.Split('-');
                    if (times.Length == 2)
                    {
                        var startTime = TimeSpan.Parse(times[0]);
                        var endTime = TimeSpan.Parse(times[1]);

                        query = query.Where(t =>
                            t.TransactionDate.TimeOfDay >= startTime &&
                            t.TransactionDate.TimeOfDay <= endTime);
                    }
                }

                var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync();

                // Apply day of week filter
                if (!string.IsNullOrEmpty(dayOfWeek))
                {
                    var dayOfWeekFull = dayOfWeek.ToLower() switch
                    {
                        "mon" => DayOfWeek.Monday,
                        "tue" => DayOfWeek.Tuesday,
                        "wed" => DayOfWeek.Wednesday,
                        "thu" => DayOfWeek.Thursday,
                        "fri" => DayOfWeek.Friday,
                        "sat" => DayOfWeek.Saturday,
                        "sun" => DayOfWeek.Sunday,
                        _ => throw new ArgumentException($"Invalid dayOfWeek value: {dayOfWeek}")
                    };

                    transactions = transactions
                        .Where(t => t.TransactionDate.DayOfWeek == dayOfWeekFull)
                        .ToList();
                }

                return _mapper.Map<IEnumerable<TransactionDetailDTO>>(transactions);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching transactions");
                throw;
            }
        }

        public async Task<DailySummaryDTO> GetDailySummaryAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation("Generating daily summary for date {Date}", date);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfDay && t.TransactionDate <= endOfDay && userWalletIds.Contains(t.WalletID))
                    .ToListAsync();

                var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));

                var result = new DailySummaryDTO
                {
                    Date = date.Date,
                    DayOfWeek = date.DayOfWeek.ToString(),
                    Month = date.ToString("MMMM"),
                    TotalIncome = income,
                    TotalExpenses = expenses,
                    Transactions = _mapper.Map<List<TransactionDetailDTO>>(transactions)
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily summary");
                throw;
            }
        }

        public async Task<WeeklySummaryDTO> GetWeeklySummaryAsync(DateTime weekStartDate)
        {
            try
            {
                _logger.LogInformation("Generating weekly summary for week starting {Date}", weekStartDate);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var startOfWeek = weekStartDate.Date;
                var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfWeek && t.TransactionDate <= endOfWeek && userWalletIds.Contains(t.WalletID))
                    .OrderBy(t => t.TransactionDate)
                    .ToListAsync();

                var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
                var netCashFlow = income - expenses;

                var calendar = CultureInfo.CurrentCulture.Calendar;
                //var weekNumber = calendar.GetWeekOfYear(startOfWeek, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                var weekOfMonth = (startOfWeek.Day - 1) / 7 + 1;
                var dailyTotals = transactions
                    .GroupBy(t => t.TransactionDate.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                var dailyIncomeTotals = transactions
                    .Where(t => t.Amount > 0)
                    .GroupBy(t => t.TransactionDate.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                var dailyExpenseTotals = transactions
                    .Where(t => t.Amount < 0)
                    .GroupBy(t => t.TransactionDate.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(t => t.Amount)));

                var result = new WeeklySummaryDTO
                {
                    StartDate = startOfWeek,
                    EndDate = endOfWeek,
                    WeekNumber = weekOfMonth,
                    Year = startOfWeek.Year,
                    TotalIncome = income,
                    TotalExpenses = expenses,
                    NetCashFlow = netCashFlow,
                    Transactions = _mapper.Map<List<TransactionDetailDTO>>(transactions),
                    DailyTotals = dailyTotals,
                    DailyIncomeTotals = dailyIncomeTotals,
                    DailyExpenseTotals = dailyExpenseTotals
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating weekly summary");
                throw;
            }
        }


        public async Task<MonthlySummaryDTO> GetMonthlySummaryAsync(DateTime yearMonth)
        {
            try
            {
                _logger.LogInformation("Generating monthly summary for {Year}-{Month}", yearMonth.Year, yearMonth.Month);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var startOfMonth = new DateTime(yearMonth.Year, yearMonth.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfMonth && t.TransactionDate <= endOfMonth && userWalletIds.Contains(t.WalletID))
                    .OrderBy(t => t.TransactionDate)
                    .ToListAsync();

                var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
                var netCashFlow = income - expenses;

                var dailyTotals = transactions
                    .GroupBy(t => t.TransactionDate.Day)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                var categoryTotals = transactions
                    .GroupBy(t => t.Category.Name)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                var result = new MonthlySummaryDTO
                {
                    Month = yearMonth.Month,
                    Year = yearMonth.Year,
                    MonthName = yearMonth.ToString("MMMM"),
                    TotalIncome = income,
                    TotalExpenses = expenses,
                    NetCashFlow = netCashFlow,
                    Transactions = _mapper.Map<List<TransactionDetailDTO>>(transactions),
                    DailyTotals = dailyTotals,
                    CategoryTotals = categoryTotals
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly summary");
                throw;
            }
        }


        public async Task<YearlySummaryDTO> GetYearlySummaryAsync(int year)
        {
            try
            {
                _logger.LogInformation("Generating yearly summary for year {Year}", year);

                var userId = GetCurrentUserId();

                // Get all wallets owned by the current user
                var userWalletIds = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => w.WalletID)
                    .ToListAsync();

                var startOfYear = new DateTime(year, 1, 1);
                var endOfYear = new DateTime(year, 12, 31, 23, 59, 59, 999);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfYear && t.TransactionDate <= endOfYear && userWalletIds.Contains(t.WalletID))
                    .OrderBy(t => t.TransactionDate)
                    .ToListAsync();

                var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
                var netCashFlow = income - expenses;

                var monthlyTotals = transactions
                    .GroupBy(t => new { t.TransactionDate.Month, MonthName = t.TransactionDate.ToString("MMMM") })
                    .ToDictionary(g => g.Key.MonthName, g => g.Sum(t => t.Amount));

                var categoryTotals = transactions
                    .GroupBy(t => t.Category.Name)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                var quarterlyTotals = new Dictionary<string, decimal>
                {
                    { "Q1", transactions.Where(t => t.TransactionDate.Month >= 1 && t.TransactionDate.Month <= 3).Sum(t => t.Amount) },
                    { "Q2", transactions.Where(t => t.TransactionDate.Month >= 4 && t.TransactionDate.Month <= 6).Sum(t => t.Amount) },
                    { "Q3", transactions.Where(t => t.TransactionDate.Month >= 7 && t.TransactionDate.Month <= 9).Sum(t => t.Amount) },
                    { "Q4", transactions.Where(t => t.TransactionDate.Month >= 10 && t.TransactionDate.Month <= 12).Sum(t => t.Amount) }
                };

                var result = new YearlySummaryDTO
                {
                    Year = year,
                    TotalIncome = income,
                    TotalExpenses = expenses,
                    NetCashFlow = netCashFlow,
                    Transactions = _mapper.Map<List<TransactionDetailDTO>>(transactions),
                    MonthlyTotals = monthlyTotals,
                    CategoryTotals = categoryTotals,
                    QuarterlyTotals = quarterlyTotals
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating yearly summary");
                throw;
            }
        }

        //TODO
        public async Task<ReportInfoDTO> GenerateReportAsync(
            DateTime startDate, DateTime endDate, string? type, string format,
            bool includeTime = false, bool includeDayMonth = false)
        {
            try
            {
                _logger.LogInformation("Generating report for period {Start} to {End}, type: {Type}, format: {Format}",
                    startDate, endDate, type, format);

                // Get transactions for the report
                var query = _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate);

                // Apply type filter (income/expense)
                if (!string.IsNullOrEmpty(type))
                {
                    bool isExpense = type.ToLower() == "expense";
                    query = query.Where(t => isExpense ? t.Amount < 0 : t.Amount > 0);
                }

                var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync();

                // Generate a unique report ID
                var reportId = new Random().Next(1000, 9999);
                var timestamp = DateTime.UtcNow;

                // In a real implementation, you would save the report data to a database or file system
                // For this example, we'll just return the metadata

                return new ReportInfoDTO
                {
                    ReportId = reportId,
                    Status = "generated",
                    DownloadUrl = $"/api/reports/{reportId}/download",
                    Format = format.ToUpper(),
                    GeneratedAt = timestamp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                throw;
            }
        }
        //TODO
        public async Task<(string fileName, string contentType, byte[] fileBytes)> DownloadReportAsync(int reportId)
        {
            try
            {
                _logger.LogInformation("Downloading report with ID: {ReportId}", reportId);

                // In a real implementation, you would retrieve the report from a database or file system
                // For this example, we'll generate a simple CSV file

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(10)  // Just take a sample for demonstration
                    .ToListAsync();

                var report = new StringBuilder();

                // Add CSV header
                report.AppendLine("id,date,time,dayOfWeek,month,amount,type,category,description,wallet");

                // Add transaction rows
                foreach (var t in transactions)
                {
                    var type = t.Amount < 0 ? "expense" : "income";
                    report.AppendLine(
                        $"{t.TransactionID}," +
                        $"{t.TransactionDate.ToString("yyyy-MM-dd")}," +
                        $"{t.TransactionDate.ToString("HH:mm:ss")}," +
                        $"{t.TransactionDate.DayOfWeek}," +
                        $"{t.TransactionDate.ToString("MMMM")}," +
                        $"{Math.Abs(t.Amount)}," +
                        $"{type}," +
                        $"{t.Category.Name}," +
                        $"{t.Description?.Replace(',', ' ')}," +
                        $"{t.Wallet.WalletName}");
                }

                var fileName = $"financial_report_{reportId}_{DateTime.UtcNow:yyyyMMdd}.csv";
                var contentType = "text/csv";
                var fileBytes = Encoding.UTF8.GetBytes(report.ToString());

                return (fileName, contentType, fileBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report");
                throw;
            }
        }

    }
}
