using CurrencyConverter.Core.Common;

namespace CurrencyConverter.Core.Domains.ValueObjects;

/// <summary>
/// Represents a value object for exchange rates.
/// </summary>
public sealed record RatesValueObject
{
    // Fields
    public double Value { get; }
    public const double MinAmountValue = 0.00001; // Minimum with 5 precision 0.00001
    public const double MaxAmountValue = 99999.99999;// Maximum with 5 precision 99999.99999

    // Constructor
    public RatesValueObject(double value)
    {
        switch (value)
        {
            case <= 0:
                throw new ArgumentException(string.Format(ErrorMessages.InputMustBePositiveMsg, "Rate"), nameof(value));
            case < MinAmountValue:
            case > MaxAmountValue:
                throw new ArgumentException(ErrorMessages.ExchangeRateValueIsOutOfRangeMsg, nameof(value));
            default:
                Value = value;
                break;
        }
    }
}