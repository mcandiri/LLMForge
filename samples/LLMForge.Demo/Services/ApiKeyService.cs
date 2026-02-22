namespace LLMForge.Demo.Services;

/// <summary>
/// Scoped service that holds API keys for the current user session.
/// Keys are never persisted to disk or configuration files.
/// </summary>
public class ApiKeyService
{
    private readonly Dictionary<string, string> _keys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _connectionStatus = new(StringComparer.OrdinalIgnoreCase);

    public string? OpenAIKey
    {
        get => GetKey("OpenAI");
        set => SetKey("OpenAI", value);
    }

    public string? AnthropicKey
    {
        get => GetKey("Anthropic");
        set => SetKey("Anthropic", value);
    }

    public string? GeminiKey
    {
        get => GetKey("Gemini");
        set => SetKey("Gemini", value);
    }

    public string? OllamaBaseUrl
    {
        get => GetKey("Ollama");
        set => SetKey("Ollama", value);
    }

    public string? GetKey(string provider)
    {
        return _keys.TryGetValue(provider, out var key) ? key : null;
    }

    public void SetKey(string provider, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            _keys.Remove(provider);
        else
            _keys[provider] = value.Trim();
    }

    public bool HasKey(string provider)
    {
        return _keys.ContainsKey(provider) && !string.IsNullOrWhiteSpace(_keys[provider]);
    }

    public void SetConnectionStatus(string provider, bool connected)
    {
        _connectionStatus[provider] = connected;
    }

    public bool? GetConnectionStatus(string provider)
    {
        return _connectionStatus.TryGetValue(provider, out var status) ? status : null;
    }

    public IReadOnlyList<string> GetConfiguredProviders()
    {
        return _keys.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                     .Select(kv => kv.Key)
                     .ToList()
                     .AsReadOnly();
    }
}
