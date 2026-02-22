using System.Diagnostics;
using LLMForge.Diagnostics;
using LLMForge.Scoring;

namespace LLMForge.Pipeline;

/// <summary>
/// Pipeline step that scores all successful provider responses.
/// </summary>
public class ScoringStep : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "Scoring";

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.ExecutionResult == null || context.HasError) return;

        var stopwatch = Stopwatch.StartNew();
        var allResponses = context.ExecutionResult.SuccessfulResponses;

        foreach (var response in allResponses)
        {
            double score;
            var breakdown = new Dictionary<string, double>();

            if (context.Scorer is WeightedScorer weighted)
            {
                var detailed = await weighted.ScoreDetailedAsync(response, allResponses, cancellationToken);
                score = detailed.Score;
                breakdown = detailed.ScoreBreakdown;
            }
            else if (context.Scorer != null)
            {
                score = await context.Scorer.ScoreAsync(response, allResponses, cancellationToken);
                breakdown[context.Scorer.Name] = score;
            }
            else
            {
                // Default scoring: just use 1.0 for all
                score = 1.0;
            }

            context.ScoredResponses.Add(new ScoredResponse
            {
                ProviderName = response.ProviderName,
                Content = response.Content,
                Score = score,
                ScoreBreakdown = breakdown,
                ResponseTime = response.Duration,
                TotalTokens = response.TotalTokens
            });
        }

        stopwatch.Stop();

        context.Events.Add(new PipelineEvent
        {
            EventType = "StepCompleted",
            StepName = Name,
            Message = $"Scored {context.ScoredResponses.Count} responses. " +
                      $"Top score: {context.ScoredResponses.MaxBy(s => s.Score)?.Score:F2}",
            Duration = stopwatch.Elapsed
        });
    }
}
