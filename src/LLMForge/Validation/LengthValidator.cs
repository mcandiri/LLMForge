namespace LLMForge.Validation;

/// <summary>
/// Validates that the response length falls within specified bounds.
/// </summary>
public class LengthValidator : IResponseValidator
{
    private readonly int? _minLength;
    private readonly int? _maxLength;

    /// <inheritdoc />
    public string Name => "Length";

    /// <summary>
    /// Initializes a new instance of the <see cref="LengthValidator"/>.
    /// </summary>
    /// <param name="minLength">Minimum response length (inclusive). Null for no minimum.</param>
    /// <param name="maxLength">Maximum response length (inclusive). Null for no maximum.</param>
    public LengthValidator(int? minLength = null, int? maxLength = null)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync(string content, CancellationToken cancellationToken = default)
    {
        var length = content?.Length ?? 0;

        if (_minLength.HasValue && length < _minLength.Value)
        {
            return Task.FromResult(ValidationResult.Failure(Name,
                $"Response length {length} is below minimum {_minLength.Value}"));
        }

        if (_maxLength.HasValue && length > _maxLength.Value)
        {
            return Task.FromResult(ValidationResult.Failure(Name,
                $"Response length {length} exceeds maximum {_maxLength.Value}"));
        }

        return Task.FromResult(ValidationResult.Success(Name));
    }
}
