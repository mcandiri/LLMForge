using LLMForge.Providers;

namespace LLMForge.Configuration;

/// <summary>
/// Root configuration options for the LLMForge orchestrator.
/// </summary>
public class ForgeOptions
{
    internal List<ProviderRegistration> ProviderRegistrations { get; } = new();

    /// <summary>
    /// Gets or sets the default execution strategy.
    /// </summary>
    public ExecutionStrategy DefaultStrategy { get; set; } = ExecutionStrategy.Parallel;

    /// <summary>
    /// Gets or sets the default consensus strategy.
    /// </summary>
    public ConsensusStrategy DefaultConsensus { get; set; } = ConsensusStrategy.HighestScore;

    /// <summary>
    /// Gets or sets the default timeout in seconds for individual provider calls.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether diagnostics tracking is enabled.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = true;

    /// <summary>
    /// Registers an LLM provider with its configuration.
    /// </summary>
    /// <typeparam name="TProvider">The provider type implementing <see cref="ILLMProvider"/>.</typeparam>
    /// <param name="configure">Action to configure the provider's <see cref="ModelConfig"/>.</param>
    public void AddProvider<TProvider>(Action<ModelConfig> configure) where TProvider : class, ILLMProvider
    {
        var config = new ModelConfig();
        configure(config);
        config.ProviderName = typeof(TProvider).Name.Replace("Provider", "");

        ProviderRegistrations.Add(new ProviderRegistration
        {
            ProviderType = typeof(TProvider),
            Config = config
        });
    }
}

/// <summary>
/// Internal registration record for a provider and its configuration.
/// </summary>
internal class ProviderRegistration
{
    public required Type ProviderType { get; init; }
    public required ModelConfig Config { get; init; }
}
