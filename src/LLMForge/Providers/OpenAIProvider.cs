using System.Net.Http.Json;
using System.Text.Json;
using LLMForge.Configuration;
using Microsoft.Extensions.Logging;

namespace LLMForge.Providers;

/// <summary>
/// LLM provider for OpenAI's Chat Completions API (GPT-4o, GPT-4, GPT-3.5-turbo, etc.).
/// </summary>
public class OpenAIProvider : BaseLLMProvider
{
    private const string DefaultBaseUrl = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIProvider"/>.
    /// </summary>
    public OpenAIProvider(HttpClient httpClient, ModelConfig config, ILogger<OpenAIProvider> logger)
        : base(httpClient, config, logger)
    {
    }

    /// <inheritdoc />
    public override string Name => "OpenAI";

    /// <inheritdoc />
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(Config.ApiKey);

    /// <inheritdoc />
    protected override async Task<LLMResponse> SendRequestAsync(string prompt, string? systemPrompt, CancellationToken cancellationToken)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new { role = "system", content = systemPrompt });
        }

        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model = Config.Model,
            messages,
            max_tokens = Config.MaxTokens,
            temperature = Config.Temperature
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Config.BaseUrl ?? DefaultBaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("Authorization", $"Bearer {Config.ApiKey}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        ThrowIfNotSuccess(response, json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var content = root.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var usage = root.GetProperty("usage");

        return new LLMResponse
        {
            Content = content,
            PromptTokens = usage.GetProperty("prompt_tokens").GetInt32(),
            CompletionTokens = usage.GetProperty("completion_tokens").GetInt32()
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
