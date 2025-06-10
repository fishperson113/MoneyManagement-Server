using API.Models.DTOs;
using API.Services;
using System.Reflection;

namespace API.Services;

/// <summary>
/// Currency conversion service implementation
/// </summary>
public class CurrencyConverter : ICurrencyConverter
{
    private readonly ILogger<CurrencyConverter> _logger;

    public CurrencyConverter(ILogger<CurrencyConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Fetches current exchange rate from external service
    /// </summary>
    /// <returns>Exchange rate (1 USD = X VND)</returns>
    public async Task<decimal> FetchExchangeRateAsync()
    {
        try
        {
            _logger.LogInformation("Fetching current USD/VND exchange rate");
            
            // In a real implementation, you would call an external API like:
            // - CurrencyAPI
            // - ExchangeRate-API
            // - Fixer.io
            // - Open Exchange Rates
            
            // Simulate API call
            await Task.Delay(100);
            
            // Example static rate - in production, replace with actual API call
            decimal exchangeRate = 24500m; // 1 USD = 24,500 VND
            
            _logger.LogInformation("Retrieved exchange rate: 1 USD = {Rate} VND", exchangeRate);
            return exchangeRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate, using fallback rate");
            // Return fallback rate if API call fails
            return 24000m;
        }
    }

    /// <summary>
    /// Converts monetary values from VND to USD using reflection and pattern matching
    /// </summary>
    /// <param name="data">Data object containing monetary values</param>
    /// <param name="exchangeRate">Current exchange rate</param>
    /// <returns>Converted data object</returns>
    public object ConvertToUSD(object data, decimal exchangeRate)
    {
        if (data is null)
            return data;

        return data switch
        {
            // Handle CashFlowSummaryDTO
            CashFlowSummaryDTO cashFlow => new CashFlowSummaryDTO
            {
                TotalIncome = Math.Round(cashFlow.TotalIncome / exchangeRate, 2),
                TotalExpenses = Math.Round(cashFlow.TotalExpenses / exchangeRate, 2),
                NetCashFlow = Math.Round(cashFlow.NetCashFlow / exchangeRate, 2)
            },

            // Handle CategoryBreakdownDTO collection
            IEnumerable<CategoryBreakdownDTO> categoryBreakdowns => 
                categoryBreakdowns.Select(cb => new CategoryBreakdownDTO
                {
                    Category = cb.Category,
                    TotalIncome = Math.Round(cb.TotalIncome / exchangeRate, 2),
                    TotalExpense = Math.Round(cb.TotalExpense / exchangeRate, 2),
                    IncomePercentage = cb.IncomePercentage,
                    ExpensePercentage = cb.ExpensePercentage
                }),

            // Handle TransactionDetailDTO collection
            IEnumerable<TransactionDetailDTO> transactions =>
                transactions.Select(t => new TransactionDetailDTO
                {
                    TransactionID = t.TransactionID,
                    Amount = Math.Round(t.Amount / exchangeRate, 2),
                    Description = t.Description,
                    TransactionDate = t.TransactionDate,
                    Type = t.Type,
                    Category = t.Category,
                    WalletName = t.WalletName
                }),

            // Handle anonymous objects using reflection
            _ => ConvertAnonymousObject(data, exchangeRate)
        };
    }

    /// <summary>
    /// Converts anonymous objects containing monetary values using reflection
    /// </summary>
    private object ConvertAnonymousObject(object data, decimal exchangeRate)
    {
        var type = data.GetType();
        var properties = type.GetProperties();
        var convertedData = new Dictionary<string, object?>();

        foreach (var property in properties)
        {
            var value = property.GetValue(data);
            var propertyName = property.Name;

            // Convert monetary properties
            if (IsMonetaryProperty(propertyName) && value is decimal decimalValue)
            {
                convertedData[propertyName] = Math.Round(decimalValue / exchangeRate, 2);
            }
            // Handle nested transaction collections
            else if (propertyName == "Transactions" && value is IEnumerable<TransactionDetailDTO> transactions)
            {
                convertedData[propertyName] = ConvertToUSD(transactions, exchangeRate);
            }
            // Handle other nested objects
            else if (value is not null && !IsSimpleType(property.PropertyType))
            {
                convertedData[propertyName] = ConvertToUSD(value, exchangeRate);
            }
            else
            {
                convertedData[propertyName] = value;
            }
        }

        // Create anonymous object with converted values
        return convertedData;
    }

    /// <summary>
    /// Determines if a property name represents a monetary value
    /// </summary>
    private static bool IsMonetaryProperty(string propertyName)
    {
        var monetaryKeywords = new[] 
        { 
            "amount", "income", "expense", "total", "balance", "flow", "cost", "price", "value" 
        };
        
        return monetaryKeywords.Any(keyword => 
            propertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a type is a simple type that doesn't need recursive conversion
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(DateTime) || 
               type == typeof(decimal) ||
               type == typeof(Guid) ||
               type.IsEnum ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                IsSimpleType(type.GetGenericArguments()[0]));
    }
}
