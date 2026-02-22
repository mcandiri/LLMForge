using LLMForge.Configuration;
using LLMForge.Consensus;
using LLMForge.Retry;
using LLMForge.Validation;

namespace LLMForge.Pipeline;

/// <summary>
/// A fluent pipeline builder for configuring and executing LLM orchestration.
/// </summary>
public interface IForgePipeline
{
    /// <summary>
    /// Sets the system prompt for the pipeline.
    /// </summary>
    IForgePipeline WithSystemPrompt(string systemPrompt);

    /// <summary>
    /// Sets the user prompt for the pipeline.
    /// </summary>
    IForgePipeline WithPrompt(string prompt);

    /// <summary>
    /// Configures which providers to execute on.
    /// </summary>
    IForgePipeline ExecuteOn(Func<ProviderSelector, ProviderSelector> configure);

    /// <summary>
    /// Adds validation rules for responses.
    /// </summary>
    IForgePipeline ValidateWith(Action<ValidationBuilder> configure);

    /// <summary>
    /// Configures scoring weights.
    /// </summary>
    IForgePipeline ScoreWith(Action<ScoringBuilder> configure);

    /// <summary>
    /// Sets the consensus strategy for selecting the best response.
    /// </summary>
    IForgePipeline SelectBy(ConsensusStrategy strategy);

    /// <summary>
    /// Configures retry behavior.
    /// </summary>
    IForgePipeline WithRetry(Action<RetryOptions> configure);

    /// <summary>
    /// Executes the configured pipeline.
    /// </summary>
    Task<OrchestrationResult> ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Helper for selecting providers in the pipeline.
/// </summary>
public class ProviderSelector
{
    internal List<string>? SelectedProviders { get; private set; }
    internal bool UseAll { get; private set; }

    /// <summary>Selects all configured providers.</summary>
    public ProviderSelector All()
    {
        UseAll = true;
        return this;
    }

    /// <summary>Selects specific providers by name.</summary>
    public ProviderSelector Only(params string[] providerNames)
    {
        SelectedProviders = providerNames.ToList();
        UseAll = false;
        return this;
    }
}

/// <summary>
/// Helper for building validation rules.
/// </summary>
public class ValidationBuilder
{
    internal List<IResponseValidator> Validators { get; } = new();

    /// <summary>Adds JSON validation.</summary>
    public ValidationBuilder MustBeValidJson()
    {
        Validators.Add(new JsonSchemaValidator());
        return this;
    }

    /// <summary>Adds JSON schema validation with required properties.</summary>
    public ValidationBuilder MustMatchSchema(string[] requiredProperties)
    {
        Validators.Add(new JsonSchemaValidator(requiredProperties));
        return this;
    }

    /// <summary>Adds content filter validation requiring specific keywords.</summary>
    public ValidationBuilder MustContain(params string[] keywords)
    {
        Validators.Add(new ContentFilterValidator(mustContain: keywords));
        return this;
    }

    /// <summary>Adds max length validation.</summary>
    public ValidationBuilder MaxLength(int maxLength)
    {
        Validators.Add(new LengthValidator(maxLength: maxLength));
        return this;
    }

    /// <summary>Adds min length validation.</summary>
    public ValidationBuilder MinLength(int minLength)
    {
        Validators.Add(new LengthValidator(minLength: minLength));
        return this;
    }

    /// <summary>Adds a regex validation.</summary>
    public ValidationBuilder MustMatchPattern(string pattern)
    {
        Validators.Add(new RegexValidator(pattern));
        return this;
    }

    /// <summary>Adds a custom validator.</summary>
    public ValidationBuilder Custom(string name, Func<string, bool> validate, string errorMessage = "Custom validation failed")
    {
        Validators.Add(new CustomValidator(name, validate, errorMessage));
        return this;
    }
}

/// <summary>
/// Helper for building scoring configuration.
/// </summary>
public class ScoringBuilder
{
    internal List<(string ScorerType, double Weight)> Scorers { get; } = new();

    /// <summary>Adds validation pass scoring.</summary>
    public ScoringBuilder ValidationScore(double weight = 1.0)
    {
        Scorers.Add(("ValidationPass", weight));
        return this;
    }

    /// <summary>Adds response time scoring.</summary>
    public ScoringBuilder ResponseTime(double weight = 1.0)
    {
        Scorers.Add(("ResponseTime", weight));
        return this;
    }

    /// <summary>Adds token efficiency scoring.</summary>
    public ScoringBuilder TokenEfficiency(double weight = 1.0)
    {
        Scorers.Add(("TokenEfficiency", weight));
        return this;
    }

    /// <summary>Adds consensus alignment scoring.</summary>
    public ScoringBuilder ConsensusAlignment(double weight = 1.0)
    {
        Scorers.Add(("Consensus", weight));
        return this;
    }
}

/// <summary>
/// Retry configuration options.
/// </summary>
public class RetryOptions
{
    /// <summary>Gets or sets the maximum number of retry attempts.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Gets or sets the retry policy type.</summary>
    public RetryPolicy Policy { get; set; } = RetryPolicy.ExponentialBackoff;

    /// <summary>Gets or sets whether to retry when validation fails.</summary>
    public bool RetryOnValidationFailure { get; set; } = true;
}
