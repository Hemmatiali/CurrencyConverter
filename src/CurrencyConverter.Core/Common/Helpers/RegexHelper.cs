namespace CurrencyConverter.Core.Common.Helpers;

/// <summary>
///     Helper class containing regex patterns for common validation purposes.
/// </summary>
public static class RegexHelper
{
    /// <summary>
    ///     Regular expression pattern for validating ISO 3-letter currency codes.
    /// </summary>
    public static string Iso3LetterPattern => @"^[A-Z]{3}$"; // ISO 3-letter
}