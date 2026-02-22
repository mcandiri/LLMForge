namespace LLMForge.Validation;

/// <summary>
/// Defines a validator that checks an LLM response against specific criteria.
/// </summary>
public interface IResponseValidator
{
    /// <summary>
    /// Gets the name of this validator.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Validates the response content.
    /// </summary>
    /// <param name="content">The response content to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateAsync(string content, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a validation check.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validator name that produced this result.
    /// </summary>
    public string ValidatorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success(string validatorName) => new()
    {
        IsValid = true,
        ValidatorName = validatorName
    };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ValidationResult Failure(string validatorName, string errorMessage) => new()
    {
        IsValid = false,
        ValidatorName = validatorName,
        ErrorMessage = errorMessage
    };
}
