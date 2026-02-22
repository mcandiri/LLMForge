using System.Net.Http.Json;
using System.Text.Json;
using LLMForge.Configuration;
using Microsoft.Extensions.Logging;

namespace LLMForge.Providers;

/// <summary>
/// LLM provider for Anthropic's Messages API (Claude models).
/// </summary>
public class AnthropicProvider : BaseLLMProvider
{
    private const string DefaultBaseUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicProvider"/>.
    /// </summary>
    public AnthropicProvider(HttpClient httpClient, ModelConfig config, ILogger<AnthropicProvider> logger)
        : base(httpClient, config, logger)
    {
    }

    /// <inheritdoc />
    public override string Name => "Anthropic";

    /// <inheritdoc />
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(Config.ApiKey);

    /// <inheritdoc />
    protected override async Task<LLMResponse> SendRequestAsync(string prompt, string? systemPrompt, CancellationToken cancellationToken)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = Config.Model,
            ["max_tokens"] = Config.MaxTokens,
            ["messages"] = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            requestBody["system"] = systemPrompt;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, Config.BaseUrl ?? DefaultBaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("x-api-key", Config.ApiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var content = root.GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        var usage = root.GetProperty("usage");

        return new LLMResponse
        {
            Content = content,
            PromptTokens = usage.GetProperty("input_tokens").GetInt32(),
            CompletionTokens = usage.GetProperty("output_tokens").GetInt32()
        };
    }

    /// <inheritdoc />
    public override async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GenerateAsync("Say 'ok'", cancellationToken: cancellationToken);
            return response.IsSuccess;
        }
        catch
        {
            return false;
        }
    }
}
