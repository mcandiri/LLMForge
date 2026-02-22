using LLMForge.Configuration;

namespace LLMForge.Providers;

/// <summary>
/// Defines the contract for an LLM provider that can generate text completions.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Gets the unique name of this provider (e.g., "OpenAI", "Anthropic").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the model identifier this provider is configured to use.
    /// </summary>
    string ModelId { get; }

    /// <summary>
    /// Gets a display name combining provider and model (e.g., "OpenAI/gpt-4o").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets whether this provider is currently configured with valid credentials.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a prompt to the LLM and returns the generated response.
    /// </summary>
    /// <param name="prompt">The user prompt to send.</param>
    /// <param name="systemPrompt">An optional system prompt for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider's response including content and metadata.</returns>
    Task<LLMResponse> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the provider's API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider is reachable and credentials are valid.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a response from an LLM provider.
/// </summary>
public class LLMResponse
{
    /// <summary>
    /// Gets or sets the generated text content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name that generated this response.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model identifier used.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of tokens used in the prompt.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens in the completion.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets the total token count.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Gets or sets the response generation duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets whether the response was generated successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if the response failed.
    /// </summary>
    public string? Error { get; set; }
}
