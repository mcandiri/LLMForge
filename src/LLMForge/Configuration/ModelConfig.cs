namespace LLMForge.Configuration;

/// <summary>
/// Configuration for a specific LLM provider instance.
/// </summary>
public class ModelConfig
{
    /// <summary>
    /// Gets or sets the API key for the provider. Not required for local providers like Ollama.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the model identifier (e.g., "gpt-4o", "claude-sonnet-4-20250514").
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of tokens in the response.
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the timeout in seconds for API calls.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the base URL for the API endpoint. Used primarily for Ollama or custom endpoints.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the temperature parameter for response generation (0.0 - 2.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the provider name (auto-populated during registration).
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;
}
