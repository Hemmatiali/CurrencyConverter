using System.Text.RegularExpressions;
using CurrencyConverter.Core.Common;
using CurrencyConverter.Core.Common.Helpers;

namespace CurrencyConverter.Core.Domains.ValueObjects;

/// <summary>
/// Represents a value object for currency codes.
/// </summary>
public sealed record CurrencyCodeValueObject
{
    // Fields
    public string Value { get; }

    // Constructor
    public CurrencyCodeValueObject(string? value)
    {
        // Assuming ISO 3-letter currency codes
        if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, RegexHelper.Iso3LetterPattern))
            throw new ArgumentException(string.Format(ErrorMessages.CurrencyCodeIso3LetterMsg, nameof(CurrencyCodeValueObject)), nameof(value));

        // Normalize to uppercase
        Value = value.Trim().ToUpper();
    }
}