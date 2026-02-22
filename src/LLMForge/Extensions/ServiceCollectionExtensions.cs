using LLMForge.Configuration;
using LLMForge.Diagnostics;
using LLMForge.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LLMForge.Extensions;

/// <summary>
/// Extension methods for registering LLMForge services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LLMForge orchestration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure LLMForge options and providers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLLMForge(
        this IServiceCollection services,
        Action<ForgeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ForgeOptions();
        configure(options);

        // Register the options
        services.AddSingleton(options);

        // Register HttpClientFactory
        services.AddHttpClient();

        // Register diagnostics
        services.AddSingleton<IForgeDiagnostics, ForgeDiagnostics>();

        // Register provider registry and providers
        services.AddSingleton<ProviderRegistry>(sp =>
        {
            var registry = new ProviderRegistry();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            foreach (var registration in options.ProviderRegistrations)
            {
                var httpClient = httpClientFactory.CreateClient(registration.Config.ProviderName);
                var provider = CreateProvider(registration, httpClient, loggerFactory);
                if (provider != null)
                {
                    registry.Register(provider);
                }
            }

            return registry;
        });

        // Register the orchestrator
        services.AddSingleton<IForgeOrchestrator>(sp =>
        {
            var registry = sp.GetRequiredService<ProviderRegistry>();
            var diagnostics = sp.GetRequiredService<IForgeDiagnostics>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new ForgeOrchestrator(registry, diagnostics, loggerFactory, options);
        });

        return services;
    }

    private static ILLMProvider? CreateProvider(
        ProviderRegistration registration,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        var config = registration.Config;
        var providerType = registration.ProviderType;

        if (providerType == typeof(OpenAIProvider))
            return new OpenAIProvider(httpClient, config, loggerFactory.CreateLogger<OpenAIProvider>());

        if (providerType == typeof(AnthropicProvider))
            return new AnthropicProvider(httpClient, config, loggerFactory.CreateLogger<AnthropicProvider>());

        if (providerType == typeof(GeminiProvider))
            return new GeminiProvider(httpClient, config, loggerFactory.CreateLogger<GeminiProvider>());

        if (providerType == typeof(OllamaProvider))
            return new OllamaProvider(httpClient, config, loggerFactory.CreateLogger<OllamaProvider>());

        return null;
    }
}
