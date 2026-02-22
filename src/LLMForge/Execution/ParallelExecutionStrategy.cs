using System.Diagnostics;
using LLMForge.Providers;
using Microsoft.Extensions.Logging;

namespace LLMForge.Execution;

/// <summary>
/// Executes prompts across all providers simultaneously and collects all results.
/// </summary>
public class ParallelExecutionStrategy : IExecutionStrategy
{
    private readonly ILogger<ParallelExecutionStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelExecutionStrategy"/>.
    /// </summary>
    public ParallelExecutionStrategy(ILogger<ParallelExecutionStrategy> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ExecutionResult> ExecuteAsync(
        IReadOnlyList<ILLMProvider> providers,
        string prompt,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(providers);
        if (providers.Count == 0)
            throw new ArgumentException("At least one provider must be specified.", nameof(providers));

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing prompt in parallel across {Count} providers", providers.Count);

        var tasks = providers.Select(provider =>
            provider.GenerateAsync(prompt, systemPrompt, cancellationToken));

        var responses = await Task.WhenAll(tasks);

        stopwatch.Stop();

        var result = new ExecutionResult
        {
            TotalDuration = stopwatch.Elapsed
        };

        foreach (var response in responses)
        {
            result.Responses[response.ProviderName] = response;
        }

        _logger.LogInformation(
            "Parallel execution completed in {Duration}ms. {Success}/{Total} succeeded",
            stopwatch.ElapsedMilliseconds,
            result.SuccessfulResponses.Count,
            providers.Count);

        return result;
    }
}
