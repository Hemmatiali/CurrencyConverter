using CurrencyConverter.Core.Domains.ValueObjects;

namespace CurrencyConverter.Core.Domains.Entities;

/// <summary>
/// Represents an exchange rate between two currencies.
/// </summary>
public sealed class ExchangeRate
{
    // Fields
    public CurrencyCodeValueObject FromCurrency { get; private set; }
    public CurrencyCodeValueObject ToCurrency { get; private set; }
    public RatesValueObject Rate { get; private set; }
    public RatesValueObject InverseRate { get; private set; }

    // Constructor
    public ExchangeRate(CurrencyCodeValueObject fromCurrency, CurrencyCodeValueObject toCurrency, RatesValueObject rate, RatesValueObject inverseRate)
    {
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        Rate = rate;
        InverseRate = new RatesValueObject(1 / rate.Value); // Calculate inverse directly
    }
}