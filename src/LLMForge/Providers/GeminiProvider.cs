using System.Net.Http.Json;
using System.Text.Json;
using LLMForge.Configuration;
using Microsoft.Extensions.Logging;

namespace LLMForge.Providers;

/// <summary>
/// LLM provider for Google's Gemini API.
/// </summary>
public class GeminiProvider : BaseLLMProvider
{
    private const string BaseEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiProvider"/>.
    /// </summary>
    public GeminiProvider(HttpClient httpClient, ModelConfig config, ILogger<GeminiProvider> logger)
        : base(httpClient, config, logger)
    {
    }

    /// <inheritdoc />
    public override string Name => "Gemini";

    /// <inheritdoc />
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(Config.ApiKey);

    /// <inheritdoc />
    protected override async Task<LLMResponse> SendRequestAsync(string prompt, string? systemPrompt, CancellationToken cancellationToken)
    {
        var endpoint = Config.BaseUrl ?? $"{BaseEndpoint}/{Config.Model}:generateContent?key={Config.ApiKey}";

        var contents = new List<object>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = systemPrompt } }
            });
            contents.Add(new
            {
                role = "model",
                parts = new[] { new { text = "Understood. I will follow these instructions." } }
            });
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = prompt } }
        });

        var requestBody = new
        {
            contents,
            generationConfig = new
            {
                maxOutputTokens = Config.MaxTokens,
                temperature = Config.Temperature
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(requestBody)
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        ThrowIfNotSuccess(response, json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var content = root.GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        int promptTokens = 0, completionTokens = 0;
        if (root.TryGetProperty("usageMetadata", out var usage))
        {
            if (usage.TryGetProperty("promptTokenCount", out var pt))
                promptTokens = pt.GetInt32();
            if (usage.TryGetProperty("candidatesTokenCount", out var ct))
                completionTokens = ct.GetInt32();
        }

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
            var response = await GenerateAsync("Say 'ok'", cancellationToken: cancellationToken);
            return response.IsSuccess;
        }
        catch
        {
            return false;
        }
    }
}
