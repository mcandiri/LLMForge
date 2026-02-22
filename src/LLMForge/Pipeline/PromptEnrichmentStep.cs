using LLMForge.Diagnostics;

namespace LLMForge.Pipeline;

/// <summary>
/// Pipeline step that enriches the prompt with system context.
/// </summary>
public class PromptEnrichmentStep : IPipelineStep
{
    private readonly string? _systemPrompt;
    private readonly string? _promptPrefix;
    private readonly string? _promptSuffix;

    /// <inheritdoc />
    public string Name => "PromptEnrichment";

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEnrichmentStep"/>.
    /// </summary>
    public PromptEnrichmentStep(string? systemPrompt = null, string? promptPrefix = null, string? promptSuffix = null)
    {
        _systemPrompt = systemPrompt;
        _promptPrefix = promptPrefix;
        _promptSuffix = promptSuffix;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_systemPrompt))
        {
            context.SystemPrompt = _systemPrompt;
        }

        if (!string.IsNullOrWhiteSpace(_promptPrefix))
        {
            context.Prompt = $"{_promptPrefix}\n\n{context.Prompt}";
        }

        if (!string.IsNullOrWhiteSpace(_promptSuffix))
        {
            context.Prompt = $"{context.Prompt}\n\n{_promptSuffix}";
        }

        context.Events.Add(new PipelineEvent
        {
            EventType = "StepCompleted",
            StepName = Name,
            Message = "Prompt enrichment applied"
        });

        return Task.CompletedTask;
    }
}
