namespace LLMForge.Diagnostics;

/// <summary>
/// Represents an event that occurred during pipeline execution.
/// </summary>
public class PipelineEvent
{
    /// <summary>
    /// Gets or sets the timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the step name that generated this event.
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name, if applicable.
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Gets or sets the event message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the step.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets additional event data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}
