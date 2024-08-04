using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CurrencyConverter.Core.Common.Helpers;
using CurrencyConverter.Core.Domains.ValueObjects;

namespace CurrencyConverter.Core.Common.ValidationAttributes;

/// <summary>
///     Custom validation attribute to validate ISO 3-letter currency codes.
/// </summary>
public class ValidIso3CurrencyCodeLetterAttribute : ValidationAttribute
{
    /// <summary>
    ///     Determines whether the specified value is a valid ISO 3-letter currency codes.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True if the value is a valid ISO 3-letter currency codes; otherwise, false.</returns>
    /// <remarks>If the value is null or empty, it's considered invalid.</remarks>
    public override bool IsValid(object? value)
    {
        if (value == null) return false;
        var currencyCode = value.ToString();
        return currencyCode != null && Regex.IsMatch(currencyCode, RegexHelper.Iso3LetterPattern);
    }

    /// <summary>
    ///     Validates the specified value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A ValidationResult object.</returns>
    /// <remarks>If the value is not valid, a ValidationResult with an error message is returned.</remarks>
    protected override ValidationResult? IsValid(object value, ValidationContext validationContext)
    {
        return !IsValid(value) ? new ValidationResult(string.Format(ErrorMessages.CurrencyCodeIso3LetterMsg, nameof(CurrencyCodeValueObject))) : ValidationResult.Success;
    }
}