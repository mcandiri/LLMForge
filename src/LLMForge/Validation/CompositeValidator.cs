namespace LLMForge.Validation;

/// <summary>
/// Chains multiple validators together. All validators must pass for the composite to pass.
/// </summary>
public class CompositeValidator : IResponseValidator
{
    private readonly List<IResponseValidator> _validators = new();

    /// <inheritdoc />
    public string Name => "Composite";

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeValidator"/>.
    /// </summary>
    public CompositeValidator()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified validators.
    /// </summary>
    /// <param name="validators">The validators to chain.</param>
    public CompositeValidator(IEnumerable<IResponseValidator> validators)
    {
        _validators.AddRange(validators);
    }

    /// <summary>
    /// Adds a validator to the chain.
    /// </summary>
    /// <param name="validator">The validator to add.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public CompositeValidator Add(IResponseValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validators.Add(validator);
        return this;
    }

    /// <summary>
    /// Gets all validators in this composite.
    /// </summary>
    public IReadOnlyList<IResponseValidator> Validators => _validators.AsReadOnly();

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(string content, CancellationToken cancellationToken = default)
    {
        var results = new List<ValidationResult>();

        foreach (var validator in _validators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await validator.ValidateAsync(content, cancellationToken);
            results.Add(result);

            if (!result.IsValid)
            {
                return ValidationResult.Failure(Name,
                    $"Validator '{result.ValidatorName}' failed: {result.ErrorMessage}");
            }
        }

        return ValidationResult.Success(Name);
    }

    /// <summary>
    /// Validates and returns individual results for each validator.
    /// </summary>
    public async Task<IReadOnlyList<ValidationResult>> ValidateAllAsync(string content, CancellationToken cancellationToken = default)
    {
        var results = new List<ValidationResult>();

        foreach (var validator in _validators)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await validator.ValidateAsync(content, cancellationToken);
            results.Add(result);
        }

        return results.AsReadOnly();
    }
}
