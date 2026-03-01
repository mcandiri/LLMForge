# LLMForge

> Multi-provider LLM orchestration toolkit for .NET — because one model's answer isn't always enough.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-99%20passing-brightgreen?style=flat)](tests/LLMForge.Tests)

---

## Why LLMForge?

When your application can't afford a wrong answer from an LLM, you don't trust a single model — you orchestrate multiple models, validate their responses, and pick the best one.

LLMForge is the engine that makes this possible with clean, testable, .NET-native code.

### The Problem

```
  Your App
     |
     v
 +--------+        +----------+
 | Prompt  |------->|  GPT-4o  |---- response ----> Hope it's correct?
 +--------+        +----------+
```

One model. One chance. No validation. No fallback.

### The Solution

```
                          +-----------+
                     +--->|  OpenAI   |---+
                     |    +-----------+   |
  +--------+         |    +-----------+   |    +----------+    +---------+    +----------+
  | Prompt  |--------+--->| Anthropic |---+--->| Validate |--->|  Score  |--->|Consensus |---> Best Answer
  +--------+         |    +-----------+   |    +----------+    +---------+    +----------+
                     |    +-----------+   |
                     +--->|  Gemini   |---+
                     |    +-----------+   |
                     +--->|  Ollama   |---+
                          +-----------+
```

Multiple models. Validated responses. Weighted scoring. Consensus-based selection.

---

## Core Features

- **Multi-provider support** — OpenAI, Anthropic, Google Gemini behind a single interface
- **Automatic failover** — if one provider fails, seamlessly falls back to the next
- **Built-in retry with circuit breaker** — handles rate limits and transient failures

## Advanced Features (opt-in)

- Multi-model execution strategies (race, fallback, consensus)
- Response validation pipeline
- Weighted scoring across providers
- Usage analytics and cost tracking

---

## Design Decisions

- **No LangChain/Semantic Kernel dependency** — intentionally minimal. LLMForge does one thing: reliable LLM API calls with failover. It doesn't try to be a framework.
- **Raw HttpClient** — zero SDK dependencies means no version conflicts and full control over retry/timeout behavior.
- **Opt-in complexity** — basic usage requires 3 lines of code. Advanced features (consensus, scoring) are there when you need them.

---

## Quick Start

### 1. Add the project reference

```bash
dotnet add reference src/LLMForge/LLMForge.csproj
```

### 2. Register services

```csharp
using LLMForge.Extensions;
using LLMForge.Providers;

builder.Services.AddLLMForge(options =>
{
    options.AddProvider<OpenAIProvider>(m =>
    {
        m.ApiKey = builder.Configuration["LLM:OpenAI:Key"];
        m.Model = "gpt-4o";
        m.MaxTokens = 2000;
    });

    options.AddProvider<AnthropicProvider>(m =>
    {
        m.ApiKey = builder.Configuration["LLM:Anthropic:Key"];
        m.Model = "claude-sonnet-4-20250514";
        m.MaxTokens = 2000;
    });

    options.AddProvider<GeminiProvider>(m =>
    {
        m.ApiKey = builder.Configuration["LLM:Google:Key"];
        m.Model = "gemini-2.0-flash";
    });

    options.AddProvider<OllamaProvider>(m =>
    {
        m.BaseUrl = "http://localhost:11434";
        m.Model = "llama3";
    });
});
```

### 3. Orchestrate

```csharp
public class MyService(IForgeOrchestrator forge)
{
    public async Task<string> AskAsync(string question)
    {
        var result = await forge.OrchestrateAsync(question);

        if (result.IsSuccess)
        {
            Console.WriteLine($"Winner: {result.BestProvider} (score: {result.BestScore:F2})");
            Console.WriteLine($"Consensus: {result.AgreementCount}/{result.TotalModels} models agreed");
            return result.BestResponse;
        }

        throw new Exception(result.FailureReason);
    }
}
```

> **Note:** LLMForge never stores API keys. Users provide their own keys via configuration or at runtime.

---

## Providers

Each provider makes direct HTTP calls — no vendor SDKs, no hidden dependencies.

| Provider | Endpoint | Auth |
|----------|----------|------|
| OpenAI | `POST https://api.openai.com/v1/chat/completions` | API Key |
| Anthropic | `POST https://api.anthropic.com/v1/messages` | API Key |
| Gemini | `POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent` | API Key |
| Ollama | `POST http://localhost:11434/api/generate` | None (local) |

---

## Execution Strategies

| Strategy | Behavior | Best For |
|----------|----------|----------|
| **Parallel** | Send to all models simultaneously, compare every response | Accuracy-critical tasks |
| **Sequential** | Try each model in order, stop on first success | Cost-sensitive tasks |
| **Fallback** | Explicit fallback chain with configurable triggers | High-availability systems |

```csharp
var result = await forge.OrchestrateAsync("Explain quantum entanglement", opts =>
{
    opts.Strategy = ExecutionStrategy.Parallel;
});
```

---

## Validation

Compose validators to ensure responses meet your requirements:

```csharp
.ValidateWith(v => v
    .MustBeValidJson()
    .MustMatchSchema(new[] { "question", "options", "answer" })
    .MinLength(50)
    .MaxLength(5000)
    .MustContain("question", "answer")
    .Custom("NoMarkdown",
        content => !content.Contains("```"),
        "Response must not contain code blocks"))
```

| Validator | What It Checks |
|-----------|---------------|
| `MustBeValidJson()` | Response parses as valid JSON |
| `MustMatchSchema(props)` | JSON contains required top-level properties |
| `MustMatchPattern(regex)` | Response matches a regular expression |
| `MinLength(n)` / `MaxLength(n)` | Response length within bounds |
| `MustContain(keywords)` | Response includes required keywords |
| `Custom(name, func, msg)` | User-defined `Func<string, bool>` |

---

## Scoring

Configure weighted scoring to rank responses. Weights are normalized automatically.

```csharp
.ScoreWith(s => s
    .ValidationScore(weight: 0.4)       // How many validators passed
    .ResponseTime(weight: 0.1)          // Faster = higher score
    .TokenEfficiency(weight: 0.1)       // Fewer tokens = higher score
    .ConsensusAlignment(weight: 0.4))   // Agreement with other models
```

---

## Consensus Strategies

| Strategy | How It Works |
|----------|-------------|
| **Highest Score** | Select the response with the highest composite score |
| **Majority Vote** | Group responses by similarity, pick from the largest group |
| **Quorum** | Require a minimum number of models to agree before accepting |

```csharp
opts.Consensus = ConsensusStrategy.MajorityVote;
opts.SimilarityThreshold = 0.6;
// result.ConsensusReached  -> true
// result.AgreementCount    -> 3
// result.DissentingModels  -> ["Ollama"]
```

---

## Pipeline API

The fluent pipeline gives you fine-grained control over every stage:

```csharp
var result = await forge.CreatePipeline()
    .WithSystemPrompt("You are a medical expert. Respond only in valid JSON.")
    .WithPrompt("Define 'myocardial infarction' with causes and symptoms")
    .ExecuteOn(p => p.All())
    .ValidateWith(v => v
        .MustBeValidJson()
        .MustMatchSchema(new[] { "term", "definition", "causes", "symptoms" })
        .MaxLength(5000))
    .ScoreWith(s => s
        .ValidationScore(weight: 0.4)
        .ConsensusAlignment(weight: 0.4)
        .ResponseTime(weight: 0.1)
        .TokenEfficiency(weight: 0.1))
    .SelectBy(ConsensusStrategy.HighestScore)
    .WithRetry(r =>
    {
        r.MaxAttempts = 3;
        r.Policy = RetryPolicy.ExponentialBackoff;
        r.RetryOnValidationFailure = true;
    })
    .ExecuteAsync();
```

---

## Prompt Templates

Register reusable templates with `{{variable}}` placeholders and optional defaults:

```csharp
forge.Templates.Register("exam-question", new PromptTemplate
{
    Name = "exam-question",
    SystemPrompt = "You are a {{subject}} teacher for {{level}} students.",
    UserPrompt = "Create a {{difficulty}} question about {{topic}}. Return as JSON.",
    Defaults = new Dictionary<string, string>
    {
        ["level"] = "high school",
        ["difficulty"] = "medium"
    }
});

var result = await forge.OrchestrateFromTemplateAsync("exam-question",
    new Dictionary<string, string>
    {
        ["subject"] = "Physics",
        ["topic"] = "Newton's Laws",
        ["difficulty"] = "hard"
    });
```

---

## Retry Policies

| Policy | Behavior |
|--------|----------|
| `ExponentialBackoff` | Delay doubles each attempt with random jitter, capped at 30s |
| `FixedDelay` | Constant 2-second wait between retries |
| `RateLimitAware` | Respects `Retry-After` headers from 429 responses, falls back to exponential backoff |
| `None` | No retries (default) |

```csharp
.WithRetry(r =>
{
    r.MaxAttempts = 3;
    r.Policy = RetryPolicy.ExponentialBackoff;
    r.RetryOnValidationFailure = true;
})
```

---

## Diagnostics & Analytics

LLMForge tracks every model's performance over time:

```csharp
var analytics = forge.GetAnalytics();

foreach (var model in analytics.Models)
{
    Console.WriteLine($"{model.Name}: " +
        $"latency={model.AverageLatency.TotalMilliseconds:F0}ms, " +
        $"success={model.SuccessRate:P0}, " +
        $"wins={model.WinRate:P0}, " +
        $"score={model.AverageScore:F2}");
}
```

```
OpenAI:    latency=1230ms, success=99%, wins=42%, score=0.84
Anthropic: latency=1580ms, success=97%, wins=35%, score=0.82
Gemini:    latency=890ms,  success=95%, wins=18%, score=0.78
Ollama:    latency=3200ms, success=100%, wins=5%, score=0.71
```

---

## Demo UI

The project includes a **Blazor Server** demo app with a dark-themed UI:

```bash
cd samples/LLMForge.Demo
dotnet run
# Open http://localhost:5050
```

| Page | What It Does |
|------|-------------|
| **Home** | Enter API keys, test connections, see provider status |
| **Playground** | Send prompts, select models, see responses side-by-side with scores |
| **Pipeline Builder** | Configure validators, scoring weights, consensus — run with execution log |
| **Analytics** | Model comparison charts, win rates, latency, orchestration history |

API keys are stored in session memory only — never written to disk.

---

## Architecture

```
+-------------------------------------------------------+
|                   ForgeOrchestrator                     |
|          OrchestrateAsync  |  CreatePipeline            |
+-----------+----------+----------+----------+----------+
| Providers | Execution| Validation| Scoring  | Consensus|
|  Registry |  Engine  |  Chain    |  Engine  | Strategy |
+-----------+----------+----------+----------+----------+
| OpenAI    | Parallel | JSON      | Valid.   | Highest  |
| Anthropic | Sequent. | Regex     | Time     | Majority |
| Gemini    | Fallback | Length    | Token    | Quorum   |
| Ollama    |          | Content   | Consens. |          |
|           |          | Custom    |          |          |
+-----------+----------+----------+----------+----------+
| Templates |               Pipeline API                  |
|  Library  |  Enrich -> Execute -> Validate -> Score     |
+-----------+---------------------------------------------+
|   Retry   |  Resilience  |       Diagnostics           |
|  ExpBack  |  Circuit     |  PipelineEvents             |
|  FixDelay |  Breaker     |  PerformanceTracker         |
|  RateAwr  |  RateLimit   |                             |
+-----------+--------------+-----------------------------+
```

**Design principles:**

- Interface-first — every component is swappable and testable
- Strategy pattern — execution, validation, scoring, consensus all pluggable
- Raw HTTP — no vendor SDKs, direct REST API calls
- Thread-safe — `ConcurrentDictionary`, `lock`-based protection
- Fully async — `CancellationToken` support throughout

---

## Security

| Concern | How LLMForge Handles It |
|---------|------------------------|
| API key storage | Never stored in code or config. Provided at runtime via `ModelConfig.ApiKey` |
| Demo app keys | Session memory only. Cleared on tab close |
| Network transport | All cloud APIs over HTTPS. Ollama runs locally |
| Dependencies | Zero external LLM SDKs. Only `System.Text.Json` and `Microsoft.Extensions.*` |

---

## Background

> Started as an internal abstraction to avoid vendor lock-in when switching between OpenAI and Anthropic APIs. The failover and retry logic was added after experiencing real outages during peak exam periods on an education platform serving 1,500+ students.

---

## Testing

High test coverage on core flows (provider failover, retry logic, response validation). All tests run with mocked HTTP — no API keys needed.

```bash
dotnet test
```

---

## Project Structure

```
LLMForge/
├── src/LLMForge/              Core library
│   ├── Configuration/         Options, enums, model config
│   ├── Providers/             OpenAI, Anthropic, Gemini, Ollama
│   ├── Execution/             Parallel, Sequential, Fallback
│   ├── Validation/            JSON, Regex, Length, Content, Custom
│   ├── Scoring/               Weighted composite scoring
│   ├── Consensus/             HighestScore, MajorityVote, Quorum
│   ├── Pipeline/              Fluent pipeline builder
│   ├── Retry/                 ExponentialBackoff, FixedDelay
│   ├── Resilience/            CircuitBreaker, RateLimitInfo, RateLimitAwareRetry
│   ├── Diagnostics/           Performance tracking
│   ├── Templates/             Prompt templates
│   └── Extensions/            DI registration
├── tests/LLMForge.Tests/      Unit tests (xUnit + Moq + FluentAssertions)
├── samples/LLMForge.Demo/     Blazor Server demo UI
└── LLMForge.sln
```

---

## Getting Started

```bash
git clone https://github.com/mcandiri/LLMForge.git
cd LLMForge

# Build
dotnet build

# Run tests (no API keys needed — all mocked)
dotnet test

# Run the demo UI
dotnet run --project samples/LLMForge.Demo
```

---

## Roadmap

- [x] Semantic similarity scoring (TF-IDF + cosine similarity)
- [x] Rate limiting per provider with circuit breaker
- [x] Configurable scoring weights via `ForgeOptions`
- [x] Reflection-based provider plug-in system
- [ ] Streaming response support
- [ ] OpenTelemetry integration
- [ ] Azure OpenAI provider
- [ ] Response caching layer
- [ ] NuGet package publication

---

## License

MIT — see [LICENSE](LICENSE)
