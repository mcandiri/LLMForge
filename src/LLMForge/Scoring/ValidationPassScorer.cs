using LLMForge.Providers;
using LLMForge.Validation;

namespace LLMForge.Scoring;

/// <summary>
/// Scores responses based on how many validators they pass.
/// </summary>
public class ValidationPassScorer : IResponseScorer
{
    private readonly IReadOnlyList<IResponseValidator> _validators;

    /// <inheritdoc />
    public string Name => "ValidationPass";

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPassScorer"/>.
    /// </summary>
    /// <param name="validators">The validators to check against.</param>
    public ValidationPassScorer(IReadOnlyList<IResponseValidator> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }

    /// <inheritdoc />
    public async Task<double> ScoreAsync(
        LLMResponse response,
        IReadOnlyList<LLMResponse> allResponses,
        CancellationToken cancellationToken = default)
    {
        if (_validators.Count == 0) return 1.0;

        var passCount = 0;

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(response.Content, cancellationToken);
            if (result.IsValid) passCount++;
        }

        return (double)passCount / _validators.Count;
    }
}
