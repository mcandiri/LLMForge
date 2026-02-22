using System.Collections.Concurrent;

namespace LLMForge.Diagnostics;

/// <summary>
/// Default implementation of <see cref="IForgeDiagnostics"/>.
/// </summary>
public class ForgeDiagnostics : IForgeDiagnostics
{
    private readonly ConcurrentQueue<PipelineEvent> _events = new();
    private const int MaxEvents = 1000;

    /// <inheritdoc />
    public ModelPerformanceTracker PerformanceTracker { get; } = new();

    /// <inheritdoc />
    public void RecordEvent(PipelineEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _events.Enqueue(evt);

        // Trim old events
        while (_events.Count > MaxEvents)
        {
            _events.TryDequeue(out _);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PipelineEvent> GetRecentEvents(int count = 100)
    {
        return _events.Reverse().Take(count).ToList().AsReadOnly();
    }
}
