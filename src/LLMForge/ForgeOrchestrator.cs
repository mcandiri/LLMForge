using LLMForge.Configuration;
using LLMForge.Consensus;
using LLMForge.Diagnostics;
using LLMForge.Execution;
using LLMForge.Pipeline;
using LLMForge.Providers;
using LLMForge.Scoring;
using LLMForge.Templates;
using Microsoft.Extensions.Logging;

namespace LLMForge;

/// <summary>
/// Default implementation of <see cref="IForgeOrchestrator"/>.
/// </summary>
public class ForgeOrchestrator : IForgeOrchestrator
{
    private readonly ProviderRegistry _registry;
    private readonly IForgeDiagnostics _diagnostics;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ForgeOrchestrator> _logger;
    private readonly ForgeOptions _options;

    /// <inheritdoc />
    public PromptLibrary Templates { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgeOrchestrator"/>.
    /// </summary>
    public ForgeOrchestrator(
        ProviderRegistry registry,
        IForgeDiagnostics diagnostics,
        ILoggerFactory loggerFactory,
        ForgeOptions options)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<ForgeOrchestrator>();
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> OrchestrateAsync(
        string prompt,
        Action<OrchestrationOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var opts = new OrchestrationOptions
        {
            Strategy = _options.DefaultStrategy,
            Consensus = _options.DefaultConsensus
        };
        configure?.Invoke(opts);

        _logger.LogInformation(
            "Starting orchestration: strategy={Strategy}, consensus={Consensus}",
            opts.Strategy, opts.Consensus);

        // Resolve providers
        var providers = ResolveProviders(opts);
        if (providers.Count == 0)
        {
            return OrchestrationResult.Failed("No configured providers available");
        }

        // Build execution strategy
        IExecutionStrategy executionStrategy = opts.Strategy switch
        {
            ExecutionStrategy.Sequential => new SequentialExecutionStrategy(
                _loggerFactory.CreateLogger<SequentialExecutionStrategy>()),
            ExecutionStrategy.Fallback => new FallbackExecutionStrategy(
                _loggerFactory.CreateLogger<FallbackExecutionStrategy>(),
                opts.FallbackOn),
            _ => new ParallelExecutionStrategy(
                _loggerFactory.CreateLogger<ParallelExecutionStrategy>())
        };

        // Execute
        var executionResult = await executionStrategy.ExecuteAsync(
            providers, prompt, opts.SystemPrompt, cancellationToken);

        if (!executionResult.HasSuccessfulResponse)
        {
            return new OrchestrationResult
            {
                IsSuccess = false,
                FailureReason = "All providers failed",
                ExecutionTime = executionResult.TotalDuration,
                Failures = executionResult.FailedResponses
                    .Select(f => new ProviderFailure { Provider = f.ProviderName, Error = f.Error ?? "Unknown" })
                    .ToList()
            };
        }

        // Score responses using configurable weights
        var allResponses = executionResult.SuccessfulResponses;
        var scoredResponses = new List<ScoredResponse>();

        // Resolve weights: per-call override > global default
        var weights = opts.ScoringWeights ?? _options.DefaultScoringWeights;

        // Build a WeightedScorer from configuration
        var scorerMap = new Dictionary<string, IResponseScorer>(StringComparer.OrdinalIgnoreCase)
        {
            ["ResponseTime"] = new ResponseTimeScorer(),
            ["Consensus"] = new ConsensusScorer(),
            ["TokenEfficiency"] = new TokenEfficiencyScorer()
        };

        var weightedScorer = new WeightedScorer();
        foreach (var (key, weight) in weights)
        {
            if (scorerMap.TryGetValue(key, out var scorer))
            {
                weightedScorer.Add(scorer, weight);
            }
        }

        foreach (var response in allResponses)
        {
            var detailed = await weightedScorer.ScoreDetailedAsync(response, allResponses, cancellationToken);

            scoredResponses.Add(new ScoredResponse
            {
                ProviderName = response.ProviderName,
                Content = response.Content,
                Score = detailed.Score,
                ScoreBreakdown = detailed.ScoreBreakdown,
                ResponseTime = response.Duration,
                TotalTokens = response.TotalTokens
            });
        }

        // Apply consensus
        IConsensusStrategy consensusStrategy = opts.Consensus switch
        {
            Configuration.ConsensusStrategy.MajorityVote =>
                new MajorityVoteStrategy(opts.SimilarityThreshold),
            Configuration.ConsensusStrategy.Quorum =>
                new QuorumStrategy(opts.QuorumCount, opts.SimilarityThreshold),
            _ => new HighestScoreStrategy()
        };

        var consensusResult = await consensusStrategy.EvaluateAsync(scoredResponses, cancellationToken);

        // Track performance
        if (_options.EnableDiagnostics)
        {
            foreach (var scored in scoredResponses)
            {
                _diagnostics.PerformanceTracker.RecordSuccess(
                    scored.ProviderName,
                    scored.ResponseTime,
                    scored.Score,
                    scored.TotalTokens,
                    scored.ProviderName == consensusResult.BestProvider);
            }

            foreach (var failed in executionResult.FailedResponses)
            {
                _diagnostics.PerformanceTracker.RecordFailure(failed.ProviderName, failed.Duration);
            }
        }

        return new OrchestrationResult
        {
            IsSuccess = consensusResult.BestResponse != null,
            BestResponse = consensusResult.BestResponse ?? string.Empty,
            BestProvider = consensusResult.BestProvider ?? string.Empty,
            BestScore = consensusResult.BestScore,
            ConsensusReached = consensusResult.ConsensusReached,
            ConsensusConfidence = consensusResult.Confidence,
            AgreementCount = consensusResult.AgreementCount,
            TotalModels = consensusResult.TotalModels,
            DissentingModels = consensusResult.DissentingModels,
            AllResponses = consensusResult.AllScoredResponses,
            ExecutionTime = executionResult.TotalDuration,
            Failures = executionResult.FailedResponses
                .Select(f => new ProviderFailure { Provider = f.ProviderName, Error = f.Error ?? "Unknown" })
                .ToList()
        };
    }

    /// <inheritdoc />
    public IForgePipeline CreatePipeline()
    {
        return new ForgePipeline(_registry, _loggerFactory);
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> OrchestrateFromTemplateAsync(
        string templateName,
        IDictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        var template = Templates.Get(templateName)
            ?? throw new InvalidOperationException($"Template '{templateName}' not found");

        var (systemPrompt, userPrompt) = template.Render(variables);

        return await OrchestrateAsync(userPrompt, opts =>
        {
            if (systemPrompt != null)
                opts.SystemPrompt = systemPrompt;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public AnalyticsSnapshot GetAnalytics()
    {
        return new AnalyticsSnapshot
        {
            Models = _diagnostics.PerformanceTracker.GetAnalytics()
        };
    }

    private IReadOnlyList<ILLMProvider> ResolveProviders(OrchestrationOptions opts)
    {
        if (opts.Strategy == ExecutionStrategy.Fallback && opts.FallbackOrder is { Length: > 0 })
        {
            return _registry.GetByNames(opts.FallbackOrder);
        }

        return _registry.GetConfigured();
    }
}
