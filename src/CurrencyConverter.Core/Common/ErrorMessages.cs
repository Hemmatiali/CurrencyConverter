namespace CurrencyConverter.Core.Common;

/// <summary>
///     Static class containing error messages used in the application.
/// </summary>
/// <remarks>
///     This class provides a centralized location for storing error messages used throughout the application.
/// </remarks>
public static class ErrorMessages
{
    // General messages
    public const string ItemNotFoundMsg = "{0} not found.";
    public const string EnterValidMsg = "Enter a valid format for {0}";
    public const string InputCannotBeNullWhiteSpaceMsg = "{0} cannot be null or whitespace.";
    public const string InputMustBePositiveMsg = "{0} must be positive.";
    public const string ThereIsNoDataMsg = "There is no data for {0}.";

    // Empty or invalid format
    public const string ValueCannotBeEmptyMsg = "{0} cannot be empty.";
    public const string InvalidFormatExceptionMsg = "{0} has an invalid format.";

    // Entity
    public const string EntityCreationSuccessMsg = "{0} has been created successfully.";
    public const string EntityCreationFailedMsg = "Failed to create {0}.";

    // Validator messages
    public const string RequiredFieldMsg = "{0} is required.";
    public const string LengthBetweenMsg = "{0} must be between {1} and {2} characters.";
    public const string MaximumLengthMsg = "{0} must be a maximum of {1} characters.";
    public const string ExchangeRateValueIsOutOfRangeMsg = "Exchange rate value is out of range.";


    // Unexpected error
    public const string UnexpectedErrorMsg = "An unexpected error occurred.";

    // Currency code messages
    public const string CurrencyCodeIso3LetterMsg = "{0} must be a valid 3-letter ISO code and in upper case format.";
}