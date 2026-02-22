namespace LLMForge.Demo.Services;

/// <summary>
/// Creates ForgeOrchestrator instances using session-scoped API keys.
/// Eliminates duplicate provider setup code across Playground and PipelineBuilder pages.
/// </summary>
public class OrchestratorFactory
{
    private readonly ApiKeyService _keyService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public OrchestratorFactory(
        ApiKeyService keyService,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _keyService = keyService;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Builds a ForgeOrchestrator with providers configured from the user's session keys.
    /// </summary>
    public LLMForge.ForgeOrchestrator Create()
    {
        var registry = new LLMForge.Providers.ProviderRegistry();

        if (_keyService.HasKey("OpenAI"))
        {
            var config = new LLMForge.Configuration.ModelConfig
            {
                ApiKey = _keyService.OpenAIKey!,
                Model = "gpt-4o",
                MaxTokens = 2000,
                TimeoutSeconds = 30,
                ProviderName = "OpenAI"
            };
            registry.Register(new LLMForge.Providers.OpenAIProvider(
                _httpClientFactory.CreateClient("OpenAI"), config,
                _loggerFactory.CreateLogger<LLMForge.Providers.OpenAIProvider>()));
        }

        if (_keyService.HasKey("Anthropic"))
        {
            var config = new LLMForge.Configuration.ModelConfig
            {
                ApiKey = _keyService.AnthropicKey!,
                Model = "claude-sonnet-4-20250514",
                MaxTokens = 2000,
                TimeoutSeconds = 30,
                ProviderName = "Anthropic"
            };
            registry.Register(new LLMForge.Providers.AnthropicProvider(
                _httpClientFactory.CreateClient("Anthropic"), config,
                _loggerFactory.CreateLogger<LLMForge.Providers.AnthropicProvider>()));
        }

        if (_keyService.HasKey("Gemini"))
        {
            var config = new LLMForge.Configuration.ModelConfig
            {
                ApiKey = _keyService.GeminiKey!,
                Model = "gemini-2.0-flash",
                MaxTokens = 2000,
                TimeoutSeconds = 30,
                ProviderName = "Gemini"
            };
            registry.Register(new LLMForge.Providers.GeminiProvider(
                _httpClientFactory.CreateClient("Gemini"), config,
                _loggerFactory.CreateLogger<LLMForge.Providers.GeminiProvider>()));
        }

        var diagnostics = new LLMForge.Diagnostics.ForgeDiagnostics();
        var options = new LLMForge.Configuration.ForgeOptions { EnableDiagnostics = true };
        return new LLMForge.ForgeOrchestrator(registry, diagnostics, _loggerFactory, options);
    }

    /// <summary>
    /// Returns true if at least one cloud provider has an API key configured.
    /// </summary>
    public bool HasConfiguredProviders =>
        _keyService.HasKey("OpenAI") || _keyService.HasKey("Anthropic") || _keyService.HasKey("Gemini");
}
