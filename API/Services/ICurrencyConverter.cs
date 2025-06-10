namespace API.Services;

/// <summary>
/// Currency conversion service interface
/// </summary>
public interface ICurrencyConverter
{
    /// <summary>
    /// Fetches current exchange rate from external service
    /// </summary>
    /// <returns>Exchange rate (1 USD = X VND)</returns>
    Task<decimal> FetchExchangeRateAsync();

    /// <summary>
    /// Converts monetary values from VND to USD
    /// </summary>
    /// <param name="data">Data object containing monetary values</param>
    /// <param name="exchangeRate">Current exchange rate</param>
    /// <returns>Converted data object</returns>
    object ConvertToUSD(object data, decimal exchangeRate);
}
