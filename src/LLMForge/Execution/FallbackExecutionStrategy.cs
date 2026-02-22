using System.Diagnostics;
using LLMForge.Configuration;
using LLMForge.Providers;
using LLMForge.Validation;
using Microsoft.Extensions.Logging;

namespace LLMForge.Execution;

/// <summary>
/// Executes providers in a specified fallback order, advancing on configurable failure triggers.
/// </summary>
public class FallbackExecutionStrategy : IExecutionStrategy
{
    private readonly ILogger<FallbackExecutionStrategy> _logger;
    private readonly FallbackTrigger _triggers;
    private readonly IReadOnlyList<IResponseValidator>? _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackExecutionStrategy"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="triggers">Conditions that trigger fallback to the next provider.</param>
    /// <param name="validators">Optional validators to check responses against.</param>
    public FallbackExecutionStrategy(
        ILogger<FallbackExecutionStrategy> logger,
        FallbackTrigger triggers = FallbackTrigger.All,
        IReadOnlyList<IResponseValidator>? validators = null)
    {
        _logger = logger;
        _triggers = triggers;
        _validators = validators;
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

        _logger.LogInformation("Executing prompt with fallback chain across {Count} providers", providers.Count);

        foreach (var provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Trying provider {Provider} in fallback chain", provider.DisplayName);

            var response = await provider.GenerateAsync(prompt, systemPrompt, cancellationToken);
            result.Responses[response.ProviderName] = response;

            if (!response.IsSuccess)
            {
                var shouldFallback = response.Error?.Contains("timed out", StringComparison.OrdinalIgnoreCase) == true
                    ? _triggers.HasFlag(FallbackTrigger.Timeout)
                    : _triggers.HasFlag(FallbackTrigger.Exception);

                if (shouldFallback)
                {
                    _logger.LogWarning("Provider {Provider} failed, falling back", provider.DisplayName);
                    continue;
                }

                break;
            }

            // Check validation if validators are configured
            if (_validators is { Count: > 0 } && _triggers.HasFlag(FallbackTrigger.ValidationFailure))
            {
                var allValid = true;
                foreach (var validator in _validators)
                {
                    var validationResult = await validator.ValidateAsync(response.Content, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        allValid = false;
                        _logger.LogWarning(
                            "Provider {Provider} response failed validation: {Reason}",
                            provider.DisplayName, validationResult.ErrorMessage);
                        break;
                    }
                }

                if (!allValid)
                {
                    continue;
                }
            }

            _logger.LogInformation("Fallback chain succeeded with {Provider}", provider.DisplayName);
            break;
        }

        stopwatch.Stop();
        result.TotalDuration = stopwatch.Elapsed;

        return result;
    }
}
