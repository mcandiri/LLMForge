using System.Diagnostics;
using LLMForge.Consensus;
using LLMForge.Diagnostics;

namespace LLMForge.Pipeline;

/// <summary>
/// Pipeline step that applies a consensus strategy to select the best response.
/// </summary>
public class ConsensusStep : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "Consensus";

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.HasError || context.ScoredResponses.Count == 0) return;

        var stopwatch = Stopwatch.StartNew();

        var strategy = context.ConsensusStrategy ?? new HighestScoreStrategy();
        context.ConsensusResult = await strategy.EvaluateAsync(context.ScoredResponses, cancellationToken);

        stopwatch.Stop();

        context.Events.Add(new PipelineEvent
        {
            EventType = "StepCompleted",
            StepName = Name,
            Message = $"Consensus: {(context.ConsensusResult.ConsensusReached ? "reached" : "NOT reached")}. " +
                      $"Winner: {context.ConsensusResult.BestProvider} (score: {context.ConsensusResult.BestScore:F2}). " +
                      $"Confidence: {context.ConsensusResult.Confidence:P0}",
            Duration = stopwatch.Elapsed
        });
    }
}
