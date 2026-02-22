using System.Diagnostics;
using LLMForge.Configuration;
using Microsoft.Extensions.Logging;

namespace LLMForge.Providers;

/// <summary>
/// Base class for LLM providers that handles common concerns like timing, error handling, and logging.
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

            Logger.LogDebug("{Provider} responded in {Duration}ms", Name, stopwatch.ElapsedMilliseconds);

            return response;
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
}
