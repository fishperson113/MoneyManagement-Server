using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace API.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionRepository> _logger;

        public TransactionRepository(ApplicationDbContext context, IMapper mapper, ILogger<TransactionRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TransactionDTO> CreateTransactionAsync(CreateTransactionDTO model)
        {
            try
            {
                _logger.LogInformation("Starting transaction creation.");

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

                var transaction = await _context.Transactions.FindAsync(model.TransactionID);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found", model.TransactionID);
                    throw new KeyNotFoundException($"Transaction with ID {model.TransactionID} not found.");
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

                var transaction = await _context.Transactions.FindAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found", transactionId);
                    throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");
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
            _logger.LogInformation("Fetching all transactions from the database.");

            try
            {
                var transactions = await _context.Transactions.ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} transactions.", transactions.Count);

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
                var transaction = await _context.Transactions.FindAsync(transactionId);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found.", transactionId);
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

        public async Task<IEnumerable<AggregateStatisticsDTO>> GetAggregateStatisticsAsync(
        string period, DateTime startDate, DateTime endDate, string? type = null)
        {
            try
            {
                _logger.LogInformation("Generating aggregate statistics for period {Period}, start: {Start}, end: {End}, type: {Type}",
                    period, startDate, endDate, type);

                var query = _context.Transactions
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate);

                // Apply type filter (income/expense)
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(t => t.Type.ToLower() == type.ToLower());
                }

                var transactions = await query.ToListAsync();
                var result = new List<AggregateStatisticsDTO>();

                // Group by the specified period and type
                switch (period.ToLower())
                {
                    case "daily":
                        result = transactions
                            .GroupBy(t => new { t.TransactionDate.Date, Type = t.Type })
                            .Select(g => new AggregateStatisticsDTO
                            {
                                Period = g.Key.Date.ToString("yyyy-MM-dd"),
                                Total = g.Sum(t => t.Amount),
                                Type = g.Key.Type
                            })
                            .ToList();
                        break;

                    case "weekly":
                        result = transactions
                            .GroupBy(t => new
                            {
                                Week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                    t.TransactionDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday),
                                Type = t.Type
                            })
                            .Select(g => new AggregateStatisticsDTO
                            {
                                Period = $"{startDate.Year}-W{g.Key.Week}",
                                Total = g.Sum(t => t.Amount),
                                Type = g.Key.Type
                            })
                            .ToList();
                        break;

                    case "monthly":
                        result = transactions
                            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, Type = t.Type })
                            .Select(g => new AggregateStatisticsDTO
                            {
                                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                                Total = g.Sum(t => t.Amount),
                                Type = g.Key.Type
                            })
                            .ToList();
                        break;

                    case "yearly":
                        result = transactions
                            .GroupBy(t => new { t.TransactionDate.Year, Type = t.Type })
                            .Select(g => new AggregateStatisticsDTO
                            {
                                Period = g.Key.Year.ToString(),
                                Total = g.Sum(t => t.Amount),
                                Type = g.Key.Type
                            })
                            .ToList();
                        break;

                    default:
                        throw new ArgumentException($"Invalid period: {period}. Use 'daily', 'weekly', 'monthly', or 'yearly'.");
                }

                return result.OrderBy(r => r.Period).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating aggregate statistics");
                throw;
            }
        }

        public async Task<IEnumerable<CategoryBreakdownDTO>> GetCategoryBreakdownAsync(
            DateTime startDate, DateTime endDate, string? type = null)
        {
            try
            {
                _logger.LogInformation("Generating category breakdown for period {Start} to {End}, type: {Type}",
                    startDate, endDate, type);

                var query = _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate);

                // Apply type filter (income/expense)
                if (!string.IsNullOrEmpty(type))
                {
                    bool isExpense = type.ToLower() == "expense";
                    query = query.Where(t => isExpense ? t.Amount < 0 : t.Amount > 0);
                }

                var transactions = await query.ToListAsync();

                // Calculate total amount for percentage calculation
                var totalAmount = transactions.Sum(t => Math.Abs(t.Amount));

                // Group by category and calculate totals and percentages
                var result = transactions
                    .GroupBy(t => t.Category.Name)
                    .Select(g => new CategoryBreakdownDTO
                    {
                        Category = g.Key,
                        Total = g.Sum(t => Math.Abs(t.Amount)),
                        Percentage = totalAmount == 0 ? 0 : Math.Round(g.Sum(t => Math.Abs(t.Amount)) / totalAmount * 100, 2)
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

                var transactions = await _context.Transactions
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
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
                _logger.LogError(ex, "Error searching transactions");
                throw;
            }
        }

        public async Task<DailySummaryDTO> GetDailySummaryAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation("Generating daily summary for date {Date}", date);

                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfDay && t.TransactionDate <= endOfDay)
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

                // Ensure we start at the beginning of the provided day
                var startOfWeek = weekStartDate.Date;
                var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfWeek && t.TransactionDate <= endOfWeek)
                    .OrderBy(t => t.TransactionDate)
                    .ToListAsync();

                var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
                var netCashFlow = income - expenses;

                // Calculate week number
                var calendar = CultureInfo.CurrentCulture.Calendar;
                var weekNumber = calendar.GetWeekOfYear(
                    startOfWeek,
                    CalendarWeekRule.FirstDay,
                    DayOfWeek.Monday);

                // Calculate daily totals for the week - FIXED VERSION
                var dailyTotals = new Dictionary<string, decimal>();

                // Group by day of week directly to avoid overwriting
                foreach (var transaction in transactions)
                {
                    var dayOfWeek = transaction.TransactionDate.DayOfWeek.ToString();
                    if (dailyTotals.ContainsKey(dayOfWeek))
                        dailyTotals[dayOfWeek] += transaction.Amount;
                    else
                        dailyTotals[dayOfWeek] = transaction.Amount;
                }

                var result = new WeeklySummaryDTO
                {
                    StartDate = startOfWeek,
                    EndDate = endOfWeek,
                    WeekNumber = weekNumber,
                    Year = startOfWeek.Year,
                    TotalIncome = income,
                    TotalExpenses = expenses,
                    NetCashFlow = netCashFlow,
                    Transactions = _mapper.Map<List<TransactionDetailDTO>>(transactions),
                    DailyTotals = dailyTotals
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

                // Start of the month
                var startOfMonth = new DateTime(yearMonth.Year, yearMonth.Month, 1);
                // End of the month
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfMonth && t.TransactionDate <= endOfMonth)
                    .OrderBy(t => t.TransactionDate)
                    .ToListAsync();

                var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
                var netCashFlow = income - expenses;

                // Calculate daily totals for the month
                var dailyTotals = transactions
                    .GroupBy(t => t.TransactionDate.Day)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(t => t.Amount)
                    );

                // Calculate category totals for the month
                var categoryTotals = transactions
                    .GroupBy(t => t.Category.Name)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(t => t.Amount)
                    );

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

                // Start and end of the year
                var startOfYear = new DateTime(year, 1, 1);
                var endOfYear = new DateTime(year, 12, 31, 23, 59, 59, 999);

                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.TransactionDate >= startOfYear && t.TransactionDate <= endOfYear)
                    .OrderBy(t => t.TransactionDate)
                    .ToListAsync();

                var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
                var netCashFlow = income - expenses;

                // Calculate monthly totals for the year
                var monthlyTotals = transactions
                    .GroupBy(t => new { Month = t.TransactionDate.Month, MonthName = t.TransactionDate.ToString("MMMM") })
                    .ToDictionary(
                        g => g.Key.MonthName,
                        g => g.Sum(t => t.Amount)
                    );

                // Calculate category totals for the year
                var categoryTotals = transactions
                    .GroupBy(t => t.Category.Name)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(t => t.Amount)
                    );

                // Calculate quarterly totals
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
