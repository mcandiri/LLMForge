namespace LLMForge.Demo.Services;

/// <summary>
/// Scoped service that tracks orchestration history for the current session.
/// </summary>
public class OrchestrationHistoryService
{
    private readonly List<OrchestrationRecord> _history = new();
    private readonly object _lock = new();

    public void AddRecord(OrchestrationRecord record)
    {
        lock (_lock)
        {
            _history.Insert(0, record);
            if (_history.Count > 50)
                _history.RemoveAt(_history.Count - 1);
        }
    }

    public IReadOnlyList<OrchestrationRecord> GetHistory()
    {
        lock (_lock)
        {
            return _history.ToList().AsReadOnly();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _history.Clear();
        }
    }
}

public class OrchestrationRecord
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Prompt { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public string BestProvider { get; set; } = string.Empty;
    public string BestResponse { get; set; } = string.Empty;
    public double BestScore { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public int TotalModels { get; set; }
    public bool ConsensusReached { get; set; }
    public List<ResponseSummary> Responses { get; set; } = new();
}

public class ResponseSummary
{
    public string Provider { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int Tokens { get; set; }
    public bool IsWinner { get; set; }
}
