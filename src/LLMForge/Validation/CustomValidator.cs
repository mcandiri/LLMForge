namespace LLMForge.Validation;

/// <summary>
/// Validates responses using a user-defined function.
/// </summary>
public class CustomValidator : IResponseValidator
{
    private readonly Func<string, bool> _validationFunc;
    private readonly string _errorMessage;

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomValidator"/>.
    /// </summary>
    /// <param name="name">A descriptive name for this validator.</param>
    /// <param name="validationFunc">The validation function. Returns true if valid.</param>
    /// <param name="errorMessage">Error message to return when validation fails.</param>
    public CustomValidator(string name, Func<string, bool> validationFunc, string errorMessage = "Custom validation failed")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(validationFunc);

        Name = name;
        _validationFunc = validationFunc;
        _errorMessage = errorMessage;
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            return _validationFunc(content)
                ? Task.FromResult(ValidationResult.Success(Name))
                : Task.FromResult(ValidationResult.Failure(Name, _errorMessage));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ValidationResult.Failure(Name, $"Validation function threw: {ex.Message}"));
        }
    }
}
