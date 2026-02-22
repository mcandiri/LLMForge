using LLMForge.Providers;

namespace LLMForge.Execution;

/// <summary>
/// Defines a strategy for executing a prompt across multiple LLM providers.
/// </summary>
public interface IExecutionStrategy
{
    /// <summary>
    /// Executes the prompt across the given providers using this strategy.
    /// </summary>
    /// <param name="providers">The providers to execute against.</param>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="systemPrompt">An optional system prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated execution result.</returns>
    Task<ExecutionResult> ExecuteAsync(
        IReadOnlyList<ILLMProvider> providers,
        string prompt,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}
