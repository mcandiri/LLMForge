using System.Diagnostics;
using System.Net;
using LLMForge.Configuration;
using LLMForge.Resilience;
using Microsoft.Extensions.Logging;

namespace LLMForge.Providers;

/// <summary>
/// Base class for LLM providers that handles common concerns like timing, error handling,
/// circuit breaker integration, and logging.
/// </summary>
public abstract class BaseLLMProvider : ILLMProvider
{
    /// <summary>
    /// The HTTP client used for API calls.
    /// </summary>
    protected readonly HttpClient HttpClient;

    /// <summary>
    /// The model configuration for this provider.
    /// </summary>
    protected readonly ModelConfig Config;

    /// <summary>
    /// Logger instance for this provider.
    /// </summary>
    protected readonly ILogger Logger;

    private CircuitBreaker? _circuitBreaker;

    /// <summary>
    /// Initializes a new instance of the provider.
    /// </summary>
    protected BaseLLMProvider(HttpClient httpClient, ModelConfig config, ILogger logger)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        HttpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
    }

    /// <summary>
    /// Sets up the circuit breaker for this provider.
    /// </summary>
    public void ConfigureCircuitBreaker(CircuitBreakerOptions options)
    {
        _circuitBreaker = new CircuitBreaker(options);
    }

    /// <summary>
    /// Gets the circuit breaker for this provider, if configured.
    /// </summary>
    public CircuitBreaker? CircuitBreaker => _circuitBreaker;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public string ModelId => Config.Model;

    /// <inheritdoc />
    public string DisplayName => $"{Name}/{ModelId}";

    /// <inheritdoc />
    public abstract bool IsConfigured { get; }

    /// <inheritdoc />
    public async Task<LLMResponse> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        // Check circuit breaker
        if (_circuitBreaker != null && !_circuitBreaker.AllowRequest())
        {
            Logger.LogWarning("{Provider} circuit breaker is open — request blocked", Name);
            return new LLMResponse
            {
                ProviderName = Name,
                ModelId = ModelId,
                IsSuccess = false,
                Error = $"Circuit breaker is open for {Name}. Too many recent failures.",
                Duration = TimeSpan.Zero
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Sending prompt to {Provider} ({Model})", Name, ModelId);

            var response = await SendRequestAsync(prompt, systemPrompt, cancellationToken);

            stopwatch.Stop();
            response.Duration = stopwatch.Elapsed;
            response.ProviderName = Name;
            response.ModelId = ModelId;
            response.IsSuccess = true;

            _circuitBreaker?.RecordSuccess();

            Logger.LogDebug("{Provider} responded in {Duration}ms", Name, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (LLMProviderException ex)
        {
            stopwatch.Stop();
            _circuitBreaker?.RecordFailure();

            Logger.LogError(ex, "{Provider} request failed ({StatusCode}) after {Duration}ms: {Error}",
                Name, ex.StatusCode, stopwatch.ElapsedMilliseconds, ex.Message);

            return new LLMResponse
            {
                ProviderName = Name,
                ModelId = ModelId,
                IsSuccess = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed,
                IsRateLimited = ex.IsRateLimited,
                HttpStatusCode = ex.StatusCode
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Logger.LogWarning("{Provider} request was cancelled after {Duration}ms", Name, stopwatch.ElapsedMilliseconds);

            return new LLMResponse
            {
                ProviderName = Name,
                ModelId = ModelId,
                IsSuccess = false,
                Error = "Request was cancelled or timed out",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _circuitBreaker?.RecordFailure();
            Logger.LogError(ex, "{Provider} request failed after {Duration}ms: {Error}", Name, stopwatch.ElapsedMilliseconds, ex.Message);

            return new LLMResponse
            {
                ProviderName = Name,
                ModelId = ModelId,
                IsSuccess = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc />
    public abstract Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the actual API request to the provider. Implemented by each concrete provider.
    /// </summary>
    protected abstract Task<LLMResponse> SendRequestAsync(string prompt, string? systemPrompt, CancellationToken cancellationToken);

    /// <summary>
    /// Helper method for providers to check HTTP response status and throw <see cref="LLMProviderException"/>
    /// with rate-limit info when appropriate instead of using EnsureSuccessStatusCode.
    /// </summary>
    protected void ThrowIfNotSuccess(HttpResponseMessage response, string responseBody)
    {
        if (response.IsSuccessStatusCode)
            return;

        RateLimitInfo? rateLimitInfo = null;
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            rateLimitInfo = RateLimitInfo.FromHeaders(response.Headers);
        }

        throw new LLMProviderException(
            Name,
            response.StatusCode,
            $"{Name} API returned {(int)response.StatusCode} {response.StatusCode}: {responseBody}",
            rateLimitInfo);
    }
}
