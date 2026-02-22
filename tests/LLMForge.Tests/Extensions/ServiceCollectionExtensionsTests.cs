using FluentAssertions;
using LLMForge.Configuration;
using LLMForge.Extensions;
using LLMForge.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LLMForge.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLLMForge_RegistersBuiltInProviders_ViaReflection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLLMForge(opts =>
        {
            opts.AddProvider<OpenAIProvider>(c =>
            {
                c.ApiKey = "test-key";
                c.Model = "gpt-4o";
            });
            opts.AddProvider<AnthropicProvider>(c =>
            {
                c.ApiKey = "test-key";
                c.Model = "claude-sonnet-4-20250514";
            });
        });

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<ProviderRegistry>();

        registry.Count.Should().Be(2);
        registry.GetProvider("OpenAI").Should().NotBeNull();
        registry.GetProvider("Anthropic").Should().NotBeNull();
    }

    [Fact]
    public void AddLLMForge_RegistersCustomProvider_ViaReflection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLLMForge(opts =>
        {
            opts.AddProvider<FakeCustomProvider>(c =>
            {
                c.ApiKey = "custom-key";
                c.Model = "custom-model";
            });
        });

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<ProviderRegistry>();

        registry.Count.Should().Be(1);
        var provider = registry.GetProvider("FakeCustom");
        provider.Should().NotBeNull();
        provider!.Name.Should().Be("FakeCustom");
        provider.ModelId.Should().Be("custom-model");
    }

    [Fact]
    public void AddLLMForge_RegistersOrchestrator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLLMForge(opts =>
        {
            opts.AddProvider<OllamaProvider>(c => c.Model = "llama3");
        });

        var sp = services.BuildServiceProvider();
        var orchestrator = sp.GetRequiredService<IForgeOrchestrator>();

        orchestrator.Should().NotBeNull();
    }

    [Fact]
    public void AddLLMForge_ThrowsOnNullConfigure()
    {
        var services = new ServiceCollection();
        var act = () => services.AddLLMForge(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

/// <summary>
/// A custom provider that follows the standard constructor convention,
/// proving that reflection-based registration works for any ILLMProvider.
/// </summary>
public class FakeCustomProvider : BaseLLMProvider
{
    public FakeCustomProvider(HttpClient httpClient, ModelConfig config, ILogger<FakeCustomProvider> logger)
        : base(httpClient, config, logger)
    {
    }

    public override string Name => "FakeCustom";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(Config.ApiKey);

    protected override Task<LLMResponse> SendRequestAsync(string prompt, string? systemPrompt, CancellationToken cancellationToken)
    {
        return Task.FromResult(new LLMResponse
        {
            Content = $"Custom response to: {prompt}",
            PromptTokens = 10,
            CompletionTokens = 20
        });
    }

    public override Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
