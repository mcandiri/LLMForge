using System.Net.Http.Json;
using System.Text.Json;
using LLMForge.Configuration;
using Microsoft.Extensions.Logging;

namespace LLMForge.Providers;

/// <summary>
/// LLM provider for Ollama local models. No API key required.
/// </summary>
public class OllamaProvider : BaseLLMProvider
{
    private const string DefaultBaseUrl = "http://localhost:11434";

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/>.
    /// </summary>
    public OllamaProvider(HttpClient httpClient, ModelConfig config, ILogger<OllamaProvider> logger)
        : base(httpClient, config, logger)
    {
    }

    /// <inheritdoc />
    public override string Name => "Ollama";

    /// <inheritdoc />
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(Config.Model);

    /// <inheritdoc />
    protected override async Task<LLMResponse> SendRequestAsync(string prompt, string? systemPrompt, CancellationToken cancellationToken)
    {
        var baseUrl = Config.BaseUrl ?? DefaultBaseUrl;
        var endpoint = $"{baseUrl.TrimEnd('/')}/api/generate";

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = Config.Model,
            ["prompt"] = prompt,
            ["stream"] = false
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            requestBody["system"] = systemPrompt;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(requestBody)
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        ThrowIfNotSuccess(response, json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var content = root.GetProperty("response").GetString() ?? string.Empty;

        int promptTokens = 0, completionTokens = 0;
        if (root.TryGetProperty("prompt_eval_count", out var pt))
            promptTokens = pt.GetInt32();
        if (root.TryGetProperty("eval_count", out var ct))
            completionTokens = ct.GetInt32();

        return new LLMResponse
        {
            Content = content,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens
        };
    }

    /// <inheritdoc />
    public override async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = Config.BaseUrl ?? DefaultBaseUrl;
            var response = await HttpClient.GetAsync($"{baseUrl.TrimEnd('/')}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
