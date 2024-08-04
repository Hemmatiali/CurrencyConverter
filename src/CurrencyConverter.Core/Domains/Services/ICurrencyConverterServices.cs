using CurrencyConverter.Core.Domains.Entities;
using CurrencyConverter.Core.DTOs;

namespace CurrencyConverter.Core.Domains.Services;

/// <summary>
/// Defines operations for currency conversion services.
/// </summary>
public interface ICurrencyConverterServices
{
    /// <summary>
    /// Clears any prior configuration.
    /// </summary>
    void ClearConfiguration();

    /// <summary>
    /// Updates the configuration. Rates are inserted or replaced internally.
    /// </summary>
    /// <param name="conversionRates">The list of exchange rates to be updated.</param>
    void UpdateConfiguration(IEnumerable<ExchangeRate> conversionRates);

    /// <summary>
    /// Converts the currency based on the provided request.
    /// </summary>
    /// <param name="request">The currency conversion request.</param>
    /// <returns>A task representing the asynchronous operation with the converted amount.</returns>
    Task<decimal> Convert(CurrencyConversionRequest request);
}