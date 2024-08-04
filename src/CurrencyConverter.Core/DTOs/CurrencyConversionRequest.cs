using System.ComponentModel.DataAnnotations;
using CurrencyConverter.Core.Common.ValidationAttributes;

namespace CurrencyConverter.Core.DTOs;

/// <summary>
/// Represents a request object for currency conversion.
/// </summary>
public sealed class CurrencyConversionRequest
{
    [Required]
    [ValidIso3CurrencyCodeLetter]
    public string? FromCurrency { get; set; }

    [Required]
    [ValidIso3CurrencyCodeLetter]
    public string? ToCurrency { get; set; }
    public double Amount { get; set; }
}