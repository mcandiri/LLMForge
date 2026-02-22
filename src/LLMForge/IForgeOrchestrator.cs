using LLMForge.Consensus;
using LLMForge.Diagnostics;
using LLMForge.Pipeline;
using LLMForge.Scoring;
using LLMForge.Templates;

namespace LLMForge;

/// <summary>
/// The main entry point for LLM orchestration. Manages providers, templates, and pipeline execution.
/// </summary>
public interface IForgeOrchestrator
{
    /// <summary>
    /// Sends a prompt to all configured providers and returns the best response.
    /// </summary>
    /// <param name="prompt">The prompt to send.</param>
    /// <param name="configure">Optional configuration for this specific orchestration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration result with the best response.</returns>
    Task<OrchestrationResult> OrchestrateAsync(
        string prompt,
        Action<OrchestrationOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new fluent pipeline builder.
    /// </summary>
    IForgePipeline CreatePipeline();

    /// <summary>
    /// Orchestrates using a registered prompt template.
    /// </summary>
    /// <param name="templateName">The template name registered in the prompt library.</param>
    /// <param name="variables">Variables to substitute in the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration result.</returns>
    Task<OrchestrationResult> OrchestrateFromTemplateAsync(
        string templateName,
        IDictionary<string, string> variables,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the prompt template library.
    /// </summary>
    PromptLibrary Templates { get; }

    /// <summary>
    /// Gets model performance analytics.
    /// </summary>
    AnalyticsSnapshot GetAnalytics();
}

/// <summary>
/// Options for a single orchestration call.
/// </summary>
public class OrchestrationOptions
{
    /// <summary>Gets or sets the execution strategy.</summary>
    public Configuration.ExecutionStrategy Strategy { get; set; } = Configuration.ExecutionStrategy.Parallel;

    /// <summary>Gets or sets the consensus strategy.</summary>
    public Configuration.ConsensusStrategy Consensus { get; set; } = Configuration.ConsensusStrategy.HighestScore;

    /// <summary>Gets or sets the fallback order for fallback strategy.</summary>
    public string[]? FallbackOrder { get; set; }

    /// <summary>Gets or sets the fallback triggers.</summary>
    public Configuration.FallbackTrigger FallbackOn { get; set; } = Configuration.FallbackTrigger.All;

    /// <summary>Gets or sets the quorum count for quorum consensus.</summary>
    public int QuorumCount { get; set; } = 3;

    /// <summary>Gets or sets the similarity threshold for consensus comparison.</summary>
    public double SimilarityThreshold { get; set; } = 0.6;

    /// <summary>Gets or sets an optional system prompt.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets per-call scoring weight overrides.
    /// Keys are scorer names (e.g., "ResponseTime", "Consensus"), values are weights.
    /// When null, falls back to <see cref="Configuration.ForgeOptions.DefaultScoringWeights"/>.
    /// </summary>
    public Dictionary<string, double>? ScoringWeights { get; set; }
}

/// <summary>
/// The result of an orchestration operation.
/// </summary>
public class OrchestrationResult
{
    /// <summary>Gets or sets whether the orchestration succeeded.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Gets or sets the best response content.</summary>
    public string BestResponse { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider that produced the best response.</summary>
    public string BestProvider { get; set; } = string.Empty;

    /// <summary>Gets or sets the best response's score.</summary>
    public double BestScore { get; set; }

    /// <summary>Gets or sets whether consensus was reached.</summary>
    public bool ConsensusReached { get; set; }

    /// <summary>Gets or sets the consensus confidence level (0.0 - 1.0).</summary>
    public double ConsensusConfidence { get; set; }

    /// <summary>Gets or sets how many models agreed.</summary>
    public int AgreementCount { get; set; }

    /// <summary>Gets or sets the total number of models that participated.</summary>
    public int TotalModels { get; set; }

    /// <summary>Gets or sets the models that dissented.</summary>
    public List<string> DissentingModels { get; set; } = new();

    /// <summary>Gets or sets all scored responses.</summary>
    public List<ScoredResponse> AllResponses { get; set; } = new();

    /// <summary>Gets or sets the total pipeline execution time.</summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>Gets or sets the failure reason if orchestration failed.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Gets or sets individual provider failures.</summary>
    public List<ProviderFailure> Failures { get; set; } = new();

    /// <summary>Gets or sets the pipeline events for debugging.</summary>
    public List<Diagnostics.PipelineEvent> PipelineEvents { get; set; } = new();

    /// <summary>Creates a failed result.</summary>
    public static OrchestrationResult Failed(string reason) => new()
    {
        IsSuccess = false,
        FailureReason = reason
    };
}

/// <summary>
/// Represents a single provider failure during orchestration.
/// </summary>
public class ProviderFailure
{
    /// <summary>Gets or sets the provider name.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the error message.</summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// A snapshot of model performance analytics.
/// </summary>
public class AnalyticsSnapshot
{
    /// <summary>Gets or sets per-model analytics.</summary>
    public IReadOnlyList<ModelAnalytics> Models { get; set; } = Array.Empty<ModelAnalytics>();
}
