using System.Diagnostics;
using LLMForge.Providers;
using Microsoft.Extensions.Logging;

namespace LLMForge.Execution;

/// <summary>
/// Executes providers one at a time, stopping on the first successful response.
/// </summary>
public class SequentialExecutionStrategy : IExecutionStrategy
{
    private readonly ILogger<SequentialExecutionStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialExecutionStrategy"/>.
    /// </summary>
    public SequentialExecutionStrategy(ILogger<SequentialExecutionStrategy> logger)
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
        var result = new ExecutionResult();

        _logger.LogInformation("Executing prompt sequentially across {Count} providers", providers.Count);

        foreach (var provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Trying provider {Provider}", provider.DisplayName);

            var response = await provider.GenerateAsync(prompt, systemPrompt, cancellationToken);
            result.Responses[response.ProviderName] = response;

            if (response.IsSuccess)
            {
                _logger.LogInformation("Sequential execution succeeded with {Provider}", provider.DisplayName);
                break;
            }

            _logger.LogWarning("Provider {Provider} failed, trying next", provider.DisplayName);
        }

        stopwatch.Stop();
        result.TotalDuration = stopwatch.Elapsed;

        return result;
    }
}
