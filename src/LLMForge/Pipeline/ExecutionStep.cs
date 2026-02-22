using System.Diagnostics;
using LLMForge.Diagnostics;
using LLMForge.Execution;
using Microsoft.Extensions.Logging;

namespace LLMForge.Pipeline;

/// <summary>
/// Pipeline step that executes the prompt against configured providers.
/// </summary>
public class ExecutionStep : IPipelineStep
{
    private readonly ILogger<ExecutionStep> _logger;

    /// <inheritdoc />
    public string Name => "Execution";

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStep"/>.
    /// </summary>
    public ExecutionStep(ILogger<ExecutionStep> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.ExecutionStrategy == null)
        {
            context.HasError = true;
            context.ErrorMessage = "No execution strategy configured";
            return;
        }

        if (context.Providers.Count == 0)
        {
            context.HasError = true;
            context.ErrorMessage = "No providers configured";
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        context.ExecutionResult = await context.ExecutionStrategy.ExecuteAsync(
            context.Providers,
            context.Prompt,
            context.SystemPrompt,
            cancellationToken);

        stopwatch.Stop();

        context.Events.Add(new PipelineEvent
        {
            EventType = "StepCompleted",
            StepName = Name,
            Message = $"Executed across {context.Providers.Count} providers. " +
                      $"{context.ExecutionResult.SuccessfulResponses.Count} succeeded.",
            Duration = stopwatch.Elapsed
        });

        if (!context.ExecutionResult.HasSuccessfulResponse)
        {
            context.HasError = true;
            context.ErrorMessage = "All providers failed to generate a response";
        }
    }
}
