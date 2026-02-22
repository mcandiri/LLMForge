using System.Text.RegularExpressions;

namespace LLMForge.Validation;

/// <summary>
/// Validates that the response matches a specified regular expression pattern.
/// </summary>
public class RegexValidator : IResponseValidator
{
    private readonly Regex _regex;

    /// <inheritdoc />
    public string Name => "Regex";

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexValidator"/>.
    /// </summary>
    /// <param name="pattern">The regex pattern to match against.</param>
    /// <param name="options">Regex options.</param>
    public RegexValidator(string pattern, RegexOptions options = RegexOptions.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        _regex = new Regex(pattern, options, TimeSpan.FromSeconds(5));
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(ValidationResult.Failure(Name, "Response is empty"));
        }

        return _regex.IsMatch(content)
            ? Task.FromResult(ValidationResult.Success(Name))
            : Task.FromResult(ValidationResult.Failure(Name, $"Response does not match pattern: {_regex}"));
    }
}
