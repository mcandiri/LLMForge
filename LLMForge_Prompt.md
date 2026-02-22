# LLMForge — Claude Code Prompt

Aşağıdaki prompt'u Claude Code terminalinde çalıştır:

---

## Prompt:

Create a .NET 8 open-source library called **LLMForge** — a production-grade LLM orchestration toolkit that sends the same prompt to multiple language models simultaneously, evaluates responses, and selects the best one through configurable validation and consensus strategies.

**This is NOT a simple API wrapper.** It's an orchestration engine — the kind of system a Lead Engineer builds when a single LLM response isn't reliable enough for production.

**CRITICAL: This library does NOT include any API keys. Users provide their own keys. The demo UI includes key input fields. No hardcoded keys anywhere.**

### Project Structure:

```
LLMForge/
├── src/
│   └── LLMForge/
│       ├── LLMForge.csproj
│       ├── IForgeOrchestrator.cs
│       ├── ForgeOrchestrator.cs
│       │
│       ├── Configuration/
│       │   ├── ForgeOptions.cs
│       │   ├── ModelConfig.cs
│       │   └── OrchestratorMode.cs
│       │
│       ├── Providers/
│       │   ├── ILLMProvider.cs
│       │   ├── BaseLLMProvider.cs
│       │   ├── OpenAIProvider.cs
│       │   ├── AnthropicProvider.cs
│       │   ├── GeminiProvider.cs
│       │   ├── OllamaProvider.cs          # Local models — no API key needed
│       │   └── ProviderRegistry.cs
│       │
│       ├── Execution/
│       │   ├── IExecutionStrategy.cs
│       │   ├── ParallelExecutionStrategy.cs    # All models simultaneously
│       │   ├── SequentialExecutionStrategy.cs  # One by one, stop on success
│       │   ├── FallbackExecutionStrategy.cs    # Primary → fallback chain
│       │   └── ExecutionResult.cs
│       │
│       ├── Validation/
│       │   ├── IResponseValidator.cs
│       │   ├── JsonSchemaValidator.cs          # Response must match JSON schema
│       │   ├── RegexValidator.cs               # Response must match pattern
│       │   ├── LengthValidator.cs              # Min/max length
│       │   ├── ContentFilterValidator.cs       # Must/must not contain keywords
│       │   ├── CustomValidator.cs              # User-defined Func<string, bool>
│       │   └── CompositeValidator.cs           # Chain multiple validators
│       │
│       ├── Scoring/
│       │   ├── IResponseScorer.cs
│       │   ├── ValidationPassScorer.cs         # Score by how many validators passed
│       │   ├── ResponseTimeScorer.cs           # Faster = higher score
│       │   ├── TokenEfficiencyScorer.cs        # Same quality, fewer tokens = better
│       │   ├── ConsensusScorer.cs              # Similarity to other responses
│       │   ├── WeightedScorer.cs               # Combine multiple scorers with weights
│       │   └── ScoringResult.cs
│       │
│       ├── Consensus/
│       │   ├── IConsensusStrategy.cs
│       │   ├── MajorityVoteStrategy.cs         # Most common answer wins
│       │   ├── HighestScoreStrategy.cs         # Best scored response wins
│       │   ├── QuorumStrategy.cs               # N out of M must agree
│       │   └── ConsensusResult.cs
│       │
│       ├── Pipeline/
│       │   ├── IForgePipeline.cs
│       │   ├── ForgePipeline.cs
│       │   ├── IPipelineStep.cs
│       │   ├── PromptEnrichmentStep.cs         # Add system prompt, context
│       │   ├── ExecutionStep.cs                # Run models
│       │   ├── ValidationStep.cs               # Validate responses
│       │   ├── ScoringStep.cs                  # Score responses
│       │   ├── ConsensusStep.cs                # Pick winner
│       │   └── PipelineContext.cs
│       │
│       ├── Retry/
│       │   ├── IRetryPolicy.cs
│       │   ├── ExponentialBackoffPolicy.cs
│       │   ├── FixedDelayPolicy.cs
│       │   └── RetryContext.cs
│       │
│       ├── Diagnostics/
│       │   ├── IForgeDiagnostics.cs
│       │   ├── ForgeDiagnostics.cs
│       │   ├── PipelineEvent.cs
│       │   └── ModelPerformanceTracker.cs      # Track latency, success rate per model
│       │
│       ├── Templates/
│       │   ├── IPromptTemplate.cs
│       │   ├── PromptTemplate.cs               # Variable substitution
│       │   └── PromptLibrary.cs                # Reusable templates
│       │
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs
│
├── tests/
│   └── LLMForge.Tests/
│       ├── LLMForge.Tests.csproj
│       ├── Providers/
│       │   └── ProviderRegistryTests.cs
│       ├── Execution/
│       │   ├── ParallelExecutionTests.cs
│       │   ├── SequentialExecutionTests.cs
│       │   └── FallbackExecutionTests.cs
│       ├── Validation/
│       │   ├── JsonSchemaValidatorTests.cs
│       │   ├── CompositeValidatorTests.cs
│       │   └── ContentFilterValidatorTests.cs
│       ├── Scoring/
│       │   ├── ConsensussScorerTests.cs
│       │   ├── WeightedScorerTests.cs
│       │   └── TokenEfficiencyScorerTests.cs
│       ├── Consensus/
│       │   ├── MajorityVoteTests.cs
│       │   └── QuorumStrategyTests.cs
│       ├── Pipeline/
│       │   └── ForgePipelineTests.cs
│       └── Retry/
│           └── ExponentialBackoffTests.cs
│
├── samples/
│   └── LLMForge.Demo/                         # Blazor Server demo UI
│       ├── LLMForge.Demo.csproj
│       ├── Program.cs
│       ├── appsettings.json                    # NO API keys — empty placeholders
│       ├── Components/
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   ├── Layout/
│       │   │   └── MainLayout.razor
│       │   └── Pages/
│       │       ├── Home.razor                  # Dashboard — enter keys, run prompts
│       │       ├── Playground.razor            # Interactive prompt testing
│       │       ├── PipelineBuilder.razor        # Visual pipeline configuration
│       │       └── Analytics.razor             # Model performance comparison
│       └── wwwroot/
│           └── css/
│               └── app.css
│
├── README.md
├── LICENSE (MIT)
├── .gitignore
└── LLMForge.sln
```

### Core Concepts & Usage:

**1. Basic Setup — User Provides Their Own Keys**

```csharp
builder.Services.AddLLMForge(options =>
{
    // Each provider requires the user's own API key
    options.AddProvider<OpenAIProvider>(config =>
    {
        config.ApiKey = builder.Configuration["LLM:OpenAI:Key"]; // From user's config
        config.Model = "gpt-4o";
        config.MaxTokens = 2000;
        config.TimeoutSeconds = 30;
    });

    options.AddProvider<AnthropicProvider>(config =>
    {
        config.ApiKey = builder.Configuration["LLM:Anthropic:Key"];
        config.Model = "claude-sonnet-4-20250514";
        config.MaxTokens = 2000;
    });

    options.AddProvider<GeminiProvider>(config =>
    {
        config.ApiKey = builder.Configuration["LLM:Google:Key"];
        config.Model = "gemini-2.0-flash";
    });

    // Ollama — local model, no API key required
    options.AddProvider<OllamaProvider>(config =>
    {
        config.BaseUrl = "http://localhost:11434";
        config.Model = "llama3";
    });
});
```

**2. Simple Multi-Model Call**

```csharp
public class QuestionGenerator(IForgeOrchestrator forge)
{
    public async Task<string> GenerateQuestion(string topic)
    {
        // Sends to ALL configured models, returns the best response
        var result = await forge.OrchestrateAsync(
            $"Generate a multiple choice question about {topic}. Return as JSON with 'question', 'options', 'correctAnswer' fields.",
            options =>
            {
                options.Strategy = ExecutionStrategy.Parallel;
                options.Consensus = ConsensusStrategy.HighestScore;
            });

        // result.BestResponse        → The winning response
        // result.BestProvider         → Which model won ("OpenAI/gpt-4o")
        // result.AllResponses         → All model responses with scores
        // result.ExecutionTime        → Total pipeline duration
        // result.ConsensusConfidence  → How much models agreed (0.0 - 1.0)
        
        return result.BestResponse;
    }
}
```

**3. Pipeline with Validation**

```csharp
var result = await forge.CreatePipeline()
    .WithSystemPrompt("You are an expert educator. Always respond in valid JSON.")
    .WithPrompt("Generate a physics question for grade 10")
    .ExecuteOn(providers => providers.All())   // or .Only("OpenAI", "Anthropic")
    .ValidateWith(v =>
    {
        v.MustBeValidJson();                            // Parse check
        v.MustMatchSchema(questionSchema);              // JSON schema validation
        v.MustContain("question", "options");           // Required fields
        v.MaxLength(2000);
    })
    .ScoreWith(s =>
    {
        s.ValidationScore(weight: 0.4);                 // How many validators passed
        s.ResponseTime(weight: 0.2);                    // Speed
        s.TokenEfficiency(weight: 0.1);                 // Conciseness
        s.ConsensusAlignment(weight: 0.3);              // Agreement with others
    })
    .SelectBy(ConsensusStrategy.HighestScore)
    .WithRetry(r =>
    {
        r.MaxAttempts = 3;
        r.Policy = RetryPolicy.ExponentialBackoff;
        r.RetryOnValidationFailure = true;              // Re-prompt if validation fails
    })
    .ExecuteAsync();

if (result.IsSuccess)
{
    var question = JsonSerializer.Deserialize<Question>(result.BestResponse);
    Console.WriteLine($"Winner: {result.BestProvider} (score: {result.BestScore:F2})");
    Console.WriteLine($"Consensus confidence: {result.ConsensusConfidence:P0}");
}
else
{
    Console.WriteLine($"All models failed: {result.FailureReason}");
    foreach (var failure in result.Failures)
        Console.WriteLine($"  {failure.Provider}: {failure.Error}");
}
```

**4. Fallback Chain**

```csharp
// Try GPT-4o first, fall back to Claude, then Gemini, then local Ollama
var result = await forge.OrchestrateAsync(prompt, options =>
{
    options.Strategy = ExecutionStrategy.Fallback;
    options.FallbackOrder = new[] { "OpenAI", "Anthropic", "Gemini", "Ollama" };
    options.FallbackOn = FallbackTrigger.Timeout | FallbackTrigger.ValidationFailure;
});
```

**5. Consensus / Voting**

```csharp
// Ask 5 models, require 3 to agree
var result = await forge.OrchestrateAsync(prompt, options =>
{
    options.Strategy = ExecutionStrategy.Parallel;
    options.Consensus = ConsensusStrategy.Quorum;
    options.QuorumCount = 3;

    // How to compare responses for similarity
    options.SimilarityThreshold = 0.85;  // 85% semantic similarity = "agreement"
});

// result.ConsensusReached  → true/false
// result.AgreementCount    → How many models agreed
// result.DissentingModels  → Which ones disagreed and their responses
```

**6. Prompt Templates**

```csharp
// Define reusable templates
forge.Templates.Register("question-gen", new PromptTemplate
{
    SystemPrompt = "You are an expert educator specializing in {{subject}}.",
    UserPrompt = "Generate a {{difficulty}} level multiple choice question about {{topic}}. Include 4 options. Return as JSON.",
    Variables = new { subject = "", topic = "", difficulty = "medium" }  // Defaults
});

// Use template
var result = await forge.OrchestrateFromTemplateAsync("question-gen",
    new { subject = "Physics", topic = "Newton's Laws", difficulty = "hard" });
```

**7. Performance Analytics**

```csharp
// Built-in model performance tracking
var analytics = forge.GetAnalytics();

foreach (var model in analytics.Models)
{
    Console.WriteLine($"{model.Name}:");
    Console.WriteLine($"  Avg latency: {model.AverageLatency.TotalMilliseconds}ms");
    Console.WriteLine($"  Success rate: {model.SuccessRate:P0}");
    Console.WriteLine($"  Avg score: {model.AverageScore:F2}");
    Console.WriteLine($"  Win rate: {model.WinRate:P0}");       // How often it was selected as best
    Console.WriteLine($"  Token efficiency: {model.AvgTokensPerResponse}");
}
```

### Demo UI (Blazor Server):

The demo is a simple but professional-looking web app with 4 pages:

**Home / Dashboard:**
- API key input fields for each provider (stored in session only, never persisted)
- Connection test button per provider
- Quick status: which providers are configured and responding

**Playground:**
- Text area for prompt input
- System prompt field
- Checkboxes to select which models to use
- "Run" button
- Results panel showing all model responses side-by-side with:
  - Response text
  - Response time
  - Token count
  - Validation status (green/red badges)
  - Score breakdown
  - Winner badge on the best response

**Pipeline Builder:**
- Step-by-step pipeline configuration
- Add validators from dropdown
- Set scoring weights with sliders
- Choose consensus strategy
- Run pipeline and see detailed step-by-step execution log

**Analytics:**
- Model performance comparison charts (bar charts for latency, success rate, win rate)
- History of recent orchestrations
- Per-model breakdown

**Demo UI style:** Clean, minimal, dark-ish theme. Use Bootstrap 5 (included with Blazor). No external CSS frameworks. Professional but not over-designed.

### Technical Requirements:

- **Target:** net8.0
- **HTTP calls:** Use HttpClient with IHttpClientFactory (no 3rd party HTTP libraries)
- **Each provider implements ILLMProvider** — clean abstraction, easy to add new providers
- **All API calls are raw HTTP** — no OpenAI SDK, no Anthropic SDK, no Google SDK. Direct REST API calls. This keeps dependencies minimal and shows the developer understands the APIs at a low level.
- **Tests:** xUnit + Moq + FluentAssertions. Mock HTTP responses for provider tests — tests must run WITHOUT any API keys.
- **All async with CancellationToken support**
- **XML documentation on all public types and members**
- **Nullable reference types enabled**
- **Thread-safe** — concurrent pipeline executions must not interfere

### Provider Implementation Details:

Each provider makes direct HTTP calls to the respective API:

**OpenAI:**
- Endpoint: `https://api.openai.com/v1/chat/completions`
- Auth: `Authorization: Bearer {key}`
- Parse: `choices[0].message.content`

**Anthropic:**
- Endpoint: `https://api.anthropic.com/v1/messages`
- Auth: `x-api-key: {key}`, `anthropic-version: 2023-06-01`
- Parse: `content[0].text`

**Gemini:**
- Endpoint: `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={key}`
- Parse: `candidates[0].content.parts[0].text`

**Ollama:**
- Endpoint: `http://localhost:11434/api/generate`
- No auth needed
- Parse: `response`

### README.md Structure:

```markdown
# LLMForge

> Production-grade LLM orchestration for .NET — because one model's answer isn't always enough.

[badges]

## Why LLMForge?

When your application can't afford a wrong answer from an LLM, you don't trust a single model —
you orchestrate multiple models, validate their responses, and pick the best one.

LLMForge is the engine that makes this possible with clean, testable, .NET-native code.

### Single Model vs LLMForge

[Side-by-side: simple API call vs orchestrated pipeline with validation]

## Quick Start
## Providers (OpenAI, Anthropic, Gemini, Ollama)
## Execution Strategies (Parallel, Sequential, Fallback)
## Validation (JSON Schema, Regex, Content Filter, Custom)
## Scoring (Validation, Speed, Efficiency, Consensus)
## Consensus (Majority Vote, Highest Score, Quorum)
## Pipeline API
## Prompt Templates
## Retry Policies
## Diagnostics & Analytics
## Demo UI

## Security
- NO API keys stored in code or config
- Keys provided at runtime by the user
- Demo stores keys in session memory only
- All API calls over HTTPS

## Born From Production
> LLMForge was extracted from the AI orchestration layer of an enterprise education platform
> that uses 5 LLM models with autonomous agent architecture to generate and validate
> curriculum-aligned exam questions for 1,500+ students.

## What LLMForge Is NOT
| Need | Use Instead |
| LangChain-style agents | Semantic Kernel, LangChain |
| Vector DB / RAG | Your preferred RAG solution |
| Model fine-tuning | Provider-specific tools |
| Prompt engineering IDE | PromptFlow, LangSmith |

## Roadmap
- [ ] Streaming response support
- [ ] Semantic similarity scoring (embeddings-based)
- [ ] Rate limiting per provider
- [ ] OpenTelemetry integration
- [ ] gRPC provider support

## Contributing
## License (MIT)
```

### Code Quality Rules:

- **ZERO hardcoded API keys** — anywhere in the codebase
- **Interface-first design** — every component swappable and testable
- **Strategy pattern everywhere** — execution, validation, scoring, consensus all pluggable
- **Pipeline pattern** — composable, debuggable, extensible
- **No external LLM SDKs** — raw HTTP only (shows deep API knowledge)
- **All tests run without API keys** — mock HTTP responses
- **Defensive coding** — null checks, timeout handling, graceful degradation
- **Meaningful exceptions** — ForgeConfigurationException, ProviderTimeoutException, ValidationFailedException, ConsensusNotReachedException
- **Follow Microsoft coding conventions**
- **Thread-safe concurrent execution**

### What This Library Does NOT Do:

- ❌ No vector database / RAG — use a dedicated RAG solution
- ❌ No agent framework — this is orchestration, not agents
- ❌ No model fine-tuning — use provider tools for that
- ❌ No prompt engineering — you write the prompts, LLMForge orchestrates them
- ❌ No streaming (v1) — full response only for reliable validation
- ❌ No LangChain/.NET equivalent — intentionally simpler and more focused

---

Build the entire project. All files must compile. Tests must pass without API keys (use mocked HTTP responses). README must be compelling and professional. Demo UI must look clean and functional. Every design decision should reflect senior/lead-level engineering thinking.
