using System.Collections.Concurrent;

namespace LLMForge.Diagnostics;

/// <summary>
/// Tracks performance metrics for each LLM model over time.
/// </summary>
public class ModelPerformanceTracker
{
    private readonly ConcurrentDictionary<string, ModelMetrics> _metrics = new();

    /// <summary>
    /// Records a successful response from a model.
    /// </summary>
    public void RecordSuccess(string providerName, TimeSpan latency, double score, int tokens, bool wasWinner)
    {
        var metrics = _metrics.GetOrAdd(providerName, _ => new ModelMetrics { Name = providerName });
        lock (metrics)
        {
            metrics.TotalRequests++;
            metrics.SuccessCount++;
            metrics.TotalLatencyMs += latency.TotalMilliseconds;
            metrics.TotalScore += score;
            metrics.TotalTokens += tokens;
            if (wasWinner) metrics.WinCount++;
        }
    }

    /// <summary>
    /// Records a failed response from a model.
    /// </summary>
    public void RecordFailure(string providerName, TimeSpan latency)
    {
        var metrics = _metrics.GetOrAdd(providerName, _ => new ModelMetrics { Name = providerName });
        lock (metrics)
        {
            metrics.TotalRequests++;
            metrics.FailureCount++;
            metrics.TotalLatencyMs += latency.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Gets performance metrics for all tracked models.
    /// </summary>
    public IReadOnlyList<ModelAnalytics> GetAnalytics()
    {
        return _metrics.Values.Select(m =>
        {
            lock (m)
            {
                return new ModelAnalytics
                {
                    Name = m.Name,
                    TotalRequests = m.TotalRequests,
                    SuccessRate = m.TotalRequests > 0 ? (double)m.SuccessCount / m.TotalRequests : 0,
                    AverageLatency = m.TotalRequests > 0
                        ? TimeSpan.FromMilliseconds(m.TotalLatencyMs / m.TotalRequests)
                        : TimeSpan.Zero,
                    AverageScore = m.SuccessCount > 0 ? m.TotalScore / m.SuccessCount : 0,
                    WinRate = m.TotalRequests > 0 ? (double)m.WinCount / m.TotalRequests : 0,
                    AvgTokensPerResponse = m.SuccessCount > 0 ? m.TotalTokens / m.SuccessCount : 0
                };
            }
        }).ToList().AsReadOnly();
    }

    /// <summary>
    /// Resets all tracked metrics.
    /// </summary>
    public void Reset()
    {
        _metrics.Clear();
    }

    private class ModelMetrics
    {
        public string Name { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double TotalLatencyMs { get; set; }
        public double TotalScore { get; set; }
        public int TotalTokens { get; set; }
        public int WinCount { get; set; }
    }
}

/// <summary>
/// Aggregated analytics for a single model.
/// </summary>
public class ModelAnalytics
{
    /// <summary>Gets or sets the model/provider name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the total number of requests.</summary>
    public int TotalRequests { get; set; }

    /// <summary>Gets or sets the success rate (0.0 - 1.0).</summary>
    public double SuccessRate { get; set; }

    /// <summary>Gets or sets the average response latency.</summary>
    public TimeSpan AverageLatency { get; set; }

    /// <summary>Gets or sets the average score for successful responses.</summary>
    public double AverageScore { get; set; }

    /// <summary>Gets or sets how often this model's response was selected as best.</summary>
    public double WinRate { get; set; }

    /// <summary>Gets or sets the average completion tokens per response.</summary>
    public int AvgTokensPerResponse { get; set; }
}
