using LLMForge.Providers;

namespace LLMForge.Execution;

/// <summary>
/// Represents the result of executing a prompt across one or more LLM providers.
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// Gets or sets all responses from providers, keyed by provider name.
    /// </summary>
    public Dictionary<string, LLMResponse> Responses { get; set; } = new();

    /// <summary>
    /// Gets or sets the total execution time for all providers.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets whether at least one provider returned a successful response.
    /// </summary>
    public bool HasSuccessfulResponse => Responses.Values.Any(r => r.IsSuccess);

    /// <summary>
    /// Gets all successful responses.
    /// </summary>
    public IReadOnlyList<LLMResponse> SuccessfulResponses =>
        Responses.Values.Where(r => r.IsSuccess).ToList().AsReadOnly();

    /// <summary>
    /// Gets all failed responses.
    /// </summary>
    public IReadOnlyList<LLMResponse> FailedResponses =>
        Responses.Values.Where(r => !r.IsSuccess).ToList().AsReadOnly();
}
