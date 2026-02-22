namespace LLMForge.Validation;

/// <summary>
/// Validates that the response contains required keywords and does not contain forbidden keywords.
/// </summary>
public class ContentFilterValidator : IResponseValidator
{
    private readonly string[] _mustContain;
    private readonly string[] _mustNotContain;
    private readonly StringComparison _comparison;

    /// <inheritdoc />
    public string Name => "ContentFilter";

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentFilterValidator"/>.
    /// </summary>
    /// <param name="mustContain">Keywords that must appear in the response.</param>
    /// <param name="mustNotContain">Keywords that must not appear in the response.</param>
    /// <param name="caseSensitive">Whether keyword matching is case-sensitive.</param>
    public ContentFilterValidator(
        string[]? mustContain = null,
        string[]? mustNotContain = null,
        bool caseSensitive = false)
    {
        _mustContain = mustContain ?? Array.Empty<string>();
        _mustNotContain = mustNotContain ?? Array.Empty<string>();
        _comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(ValidationResult.Failure(Name, "Response is empty"));
        }

        foreach (var keyword in _mustContain)
        {
            if (!content.Contains(keyword, _comparison))
            {
                return Task.FromResult(ValidationResult.Failure(Name,
                    $"Response must contain '{keyword}'"));
            }
        }

        foreach (var keyword in _mustNotContain)
        {
            if (content.Contains(keyword, _comparison))
            {
                return Task.FromResult(ValidationResult.Failure(Name,
                    $"Response must not contain '{keyword}'"));
            }
        }

        return Task.FromResult(ValidationResult.Success(Name));
    }
}
