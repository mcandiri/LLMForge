using System.Diagnostics;
using LLMForge.Diagnostics;
using LLMForge.Validation;

namespace LLMForge.Pipeline;

/// <summary>
/// Pipeline step that validates all provider responses against configured validators.
/// </summary>
public class ValidationStep : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "Validation";

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.ExecutionResult == null || context.HasError) return;
        if (context.Validators.Count == 0) return;

        var stopwatch = Stopwatch.StartNew();

        foreach (var response in context.ExecutionResult.SuccessfulResponses)
        {
            var results = new List<ValidationResult>();

            foreach (var validator in context.Validators)
            {
                var result = await validator.ValidateAsync(response.Content, cancellationToken);
                results.Add(result);
            }

            context.ValidationResults[response.ProviderName] = results.AsReadOnly();
        }

        stopwatch.Stop();

        var totalValidations = context.ValidationResults.Values.Sum(r => r.Count);
        var passedValidations = context.ValidationResults.Values
            .Sum(r => r.Count(v => v.IsValid));

        context.Events.Add(new PipelineEvent
        {
            EventType = "StepCompleted",
            StepName = Name,
            Message = $"Validated {context.ValidationResults.Count} responses. " +
                      $"{passedValidations}/{totalValidations} checks passed.",
            Duration = stopwatch.Elapsed
        });
    }
}
