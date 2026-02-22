using LLMForge.Demo.Services;
using LLMForge.Extensions;
using LLMForge.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register scoped services for session state (keys never persisted)
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<OrchestrationHistoryService>();

// Register LLMForge with empty keys - users provide their own via the UI
builder.Services.AddLLMForge(options =>
{
    var config = builder.Configuration;

    options.EnableDiagnostics = true;

    // OpenAI provider (key will be empty until user provides one)
    options.AddProvider<OpenAIProvider>(m =>
    {
        m.ApiKey = config["LLM:OpenAI:Key"] ?? string.Empty;
        m.Model = "gpt-4o";
        m.MaxTokens = 2000;
        m.TimeoutSeconds = 30;
    });

    // Anthropic provider
    options.AddProvider<AnthropicProvider>(m =>
    {
        m.ApiKey = config["LLM:Anthropic:Key"] ?? string.Empty;
        m.Model = "claude-sonnet-4-20250514";
        m.MaxTokens = 2000;
        m.TimeoutSeconds = 30;
    });

    // Gemini provider
    options.AddProvider<GeminiProvider>(m =>
    {
        m.ApiKey = config["LLM:Google:Key"] ?? string.Empty;
        m.Model = "gemini-2.0-flash";
        m.MaxTokens = 2000;
        m.TimeoutSeconds = 30;
    });

    // Ollama provider (local, no API key needed)
    options.AddProvider<OllamaProvider>(m =>
    {
        m.BaseUrl = config["LLM:Ollama:BaseUrl"] ?? "http://localhost:11434";
        m.Model = "llama3";
        m.MaxTokens = 2000;
        m.TimeoutSeconds = 60;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<LLMForge.Demo.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
