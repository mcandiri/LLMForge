namespace LLMForge.Diagnostics;

/// <summary>
/// Provides diagnostics and telemetry for LLMForge pipeline executions.
/// </summary>
public interface IForgeDiagnostics
{
    /// <summary>
    /// Records a pipeline event.
    /// </summary>
    void RecordEvent(PipelineEvent evt);

    /// <summary>
    /// Gets the model performance tracker.
    /// </summary>
    ModelPerformanceTracker PerformanceTracker { get; }

    /// <summary>
    /// Gets recent pipeline events.
    /// </summary>
    IReadOnlyList<PipelineEvent> GetRecentEvents(int count = 100);
}
