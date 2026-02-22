using LLMForge.Consensus;
using LLMForge.Diagnostics;
using LLMForge.Execution;
using LLMForge.Providers;
using LLMForge.Scoring;
using LLMForge.Validation;

namespace LLMForge.Pipeline;

/// <summary>
/// Carries state through the pipeline steps.
/// </summary>
public class PipelineContext
{
    /// <summary>Gets or sets the user prompt.</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Gets or sets the system prompt.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Gets or sets the providers to use.</summary>
    public IReadOnlyList<ILLMProvider> Providers { get; set; } = Array.Empty<ILLMProvider>();

    /// <summary>Gets or sets the execution result from the execution step.</summary>
    public ExecutionResult? ExecutionResult { get; set; }

    /// <summary>Gets or sets the validation results per provider.</summary>
    public Dictionary<string, IReadOnlyList<ValidationResult>> ValidationResults { get; set; } = new();

    /// <summary>Gets or sets the scored responses.</summary>
    public List<ScoredResponse> ScoredResponses { get; set; } = new();

    /// <summary>Gets or sets the consensus result.</summary>
    public ConsensusResult? ConsensusResult { get; set; }

    /// <summary>Gets or sets the pipeline events for diagnostics.</summary>
    public List<PipelineEvent> Events { get; set; } = new();

    /// <summary>Gets or sets the validators to apply.</summary>
    public List<IResponseValidator> Validators { get; set; } = new();

    /// <summary>Gets or sets the scorer to apply.</summary>
    public IResponseScorer? Scorer { get; set; }

    /// <summary>Gets or sets the consensus strategy.</summary>
    public IConsensusStrategy? ConsensusStrategy { get; set; }

    /// <summary>Gets or sets the execution strategy.</summary>
    public IExecutionStrategy? ExecutionStrategy { get; set; }

    /// <summary>Gets or sets whether the pipeline has encountered a fatal error.</summary>
    public bool HasError { get; set; }

    /// <summary>Gets or sets the error message if the pipeline failed.</summary>
    public string? ErrorMessage { get; set; }
}
