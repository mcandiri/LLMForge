using LLMForge.Configuration;
using LLMForge.Consensus;
using LLMForge.Execution;
using LLMForge.Providers;
using LLMForge.Resilience;
using LLMForge.Retry;
using LLMForge.Scoring;
using LLMForge.Validation;
using Microsoft.Extensions.Logging;

namespace LLMForge.Pipeline;

/// <summary>
/// Default implementation of the fluent pipeline builder.
/// </summary>
public class ForgePipeline : IForgePipeline
{
    private readonly ProviderRegistry _registry;
    private readonly ILoggerFactory _loggerFactory;
    private string _prompt = string.Empty;
    private string? _systemPrompt;
    private ProviderSelector? _providerSelector;
    private ValidationBuilder? _validationBuilder;
    private ScoringBuilder? _scoringBuilder;
    private ConsensusStrategy _consensusStrategy = Configuration.ConsensusStrategy.HighestScore;
    private RetryOptions? _retryOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgePipeline"/>.
    /// </summary>
    public ForgePipeline(ProviderRegistry registry, ILoggerFactory loggerFactory)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IForgePipeline WithSystemPrompt(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        return this;
    }

    /// <inheritdoc />
    public IForgePipeline WithPrompt(string prompt)
    {
        _prompt = prompt;
        return this;
    }

    /// <inheritdoc />
    public IForgePipeline ExecuteOn(Func<ProviderSelector, ProviderSelector> configure)
    {
        _providerSelector = configure(new ProviderSelector());
        return this;
    }

    /// <inheritdoc />
    public IForgePipeline ValidateWith(Action<ValidationBuilder> configure)
    {
        _validationBuilder = new ValidationBuilder();
        configure(_validationBuilder);
        return this;
    }

    /// <inheritdoc />
    public IForgePipeline ScoreWith(Action<ScoringBuilder> configure)
    {
        _scoringBuilder = new ScoringBuilder();
        configure(_scoringBuilder);
        return this;
    }

    /// <inheritdoc />
    public IForgePipeline SelectBy(ConsensusStrategy strategy)
    {
        _consensusStrategy = strategy;
        return this;
    }

    /// <inheritdoc />
    public IForgePipeline WithRetry(Action<RetryOptions> configure)
    {
        _retryOptions = new RetryOptions();
        configure(_retryOptions);
        return this;
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var maxAttempts = _retryOptions?.MaxAttempts ?? 1;
        IRetryPolicy? retryPolicy = _retryOptions?.Policy switch
        {
            Configuration.RetryPolicy.ExponentialBackoff => new ExponentialBackoffPolicy(),
            Configuration.RetryPolicy.FixedDelay => new FixedDelayPolicy(),
            Configuration.RetryPolicy.RateLimitAware => new RateLimitAwareRetryPolicy(),
            _ => null
        };

        OrchestrationResult? lastResult = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = BuildContext();

            // Run pipeline steps
            var steps = BuildSteps();
            foreach (var step in steps)
            {
                if (context.HasError) break;
                await step.ExecuteAsync(context, cancellationToken);
            }

            lastResult = BuildResult(context);

            if (lastResult.IsSuccess) return lastResult;

            // Check if we should retry
            if (attempt < maxAttempts && retryPolicy != null)
            {
                var retryContext = new RetryContext
                {
                    AttemptNumber = attempt,
                    MaxAttempts = maxAttempts,
                    LastError = lastResult.FailureReason
                };

                var delay = await retryPolicy.GetDelayAsync(retryContext, cancellationToken);
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value, cancellationToken);
                }
            }
        }

        return lastResult ?? OrchestrationResult.Failed("Pipeline did not produce a result");
    }

    private PipelineContext BuildContext()
    {
        // Resolve providers
        IReadOnlyList<ILLMProvider> providers;
        if (_providerSelector?.UseAll == true || _providerSelector == null)
        {
            providers = _registry.GetConfigured();
        }
        else if (_providerSelector.SelectedProviders != null)
        {
            providers = _registry.GetByNames(_providerSelector.SelectedProviders.ToArray());
        }
        else
        {
            providers = _registry.GetConfigured();
        }

        // Build scorer
        IResponseScorer? scorer = null;
        if (_scoringBuilder?.Scorers.Count > 0)
        {
            var validators = _validationBuilder?.Validators ?? new List<IResponseValidator>();
            var weighted = new WeightedScorer();
            foreach (var (type, weight) in _scoringBuilder.Scorers)
            {
                IResponseScorer s = type switch
                {
                    "ValidationPass" => new ValidationPassScorer(validators),
                    "ResponseTime" => new ResponseTimeScorer(),
                    "TokenEfficiency" => new TokenEfficiencyScorer(),
                    "Consensus" => new ConsensusScorer(),
                    _ => throw new InvalidOperationException($"Unknown scorer type: {type}")
                };
                weighted.Add(s, weight);
            }
            scorer = weighted;
        }

        // Build consensus strategy
        IConsensusStrategy consensus = _consensusStrategy switch
        {
            Configuration.ConsensusStrategy.MajorityVote => new MajorityVoteStrategy(),
            Configuration.ConsensusStrategy.Quorum => new QuorumStrategy(),
            _ => new HighestScoreStrategy()
        };

        return new PipelineContext
        {
            Prompt = _prompt,
            SystemPrompt = _systemPrompt,
            Providers = providers,
            Validators = _validationBuilder?.Validators ?? new List<IResponseValidator>(),
            Scorer = scorer,
            ConsensusStrategy = consensus,
            ExecutionStrategy = new ParallelExecutionStrategy(
                _loggerFactory.CreateLogger<ParallelExecutionStrategy>())
        };
    }

    private List<IPipelineStep> BuildSteps()
    {
        var steps = new List<IPipelineStep>();

        if (!string.IsNullOrWhiteSpace(_systemPrompt))
        {
            steps.Add(new PromptEnrichmentStep(_systemPrompt));
        }

        steps.Add(new ExecutionStep(_loggerFactory.CreateLogger<ExecutionStep>()));
        steps.Add(new ValidationStep());
        steps.Add(new ScoringStep());
        steps.Add(new ConsensusStep());

        return steps;
    }

    private static OrchestrationResult BuildResult(PipelineContext context)
    {
        if (context.HasError)
        {
            return OrchestrationResult.Failed(context.ErrorMessage ?? "Pipeline execution failed");
        }

        if (context.ConsensusResult == null)
        {
            return OrchestrationResult.Failed("No consensus result produced");
        }

        var cr = context.ConsensusResult;

        return new OrchestrationResult
        {
            IsSuccess = cr.BestResponse != null,
            BestResponse = cr.BestResponse ?? string.Empty,
            BestProvider = cr.BestProvider ?? string.Empty,
            BestScore = cr.BestScore,
            ConsensusReached = cr.ConsensusReached,
            ConsensusConfidence = cr.Confidence,
            AgreementCount = cr.AgreementCount,
            TotalModels = cr.TotalModels,
            DissentingModels = cr.DissentingModels,
            AllResponses = cr.AllScoredResponses,
            ExecutionTime = context.ExecutionResult?.TotalDuration ?? TimeSpan.Zero,
            PipelineEvents = context.Events,
            Failures = context.ExecutionResult?.FailedResponses
                .Select(f => new ProviderFailure { Provider = f.ProviderName, Error = f.Error ?? "Unknown error" })
                .ToList() ?? new List<ProviderFailure>()
        };
    }
}
