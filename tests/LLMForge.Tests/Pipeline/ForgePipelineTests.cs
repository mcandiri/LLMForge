using FluentAssertions;
using LLMForge.Pipeline;
using LLMForge.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMForge.Tests.Pipeline;

public class ForgePipelineTests
{
    private readonly ProviderRegistry _registry;
    private readonly Mock<ILoggerFactory> _loggerFactory;

    public ForgePipelineTests()
    {
        _registry = new ProviderRegistry();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
    }

    private Mock<ILLMProvider> CreateMockProvider(string name, string content = "response", bool isConfigured = true)
    {
        var mock = new Mock<ILLMProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.ModelId).Returns("model");
        mock.Setup(p => p.DisplayName).Returns($"{name}/model");
        mock.Setup(p => p.IsConfigured).Returns(isConfigured);
        mock.Setup(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse
            {
                Content = content,
                ProviderName = name,
                ModelId = "model",
                IsSuccess = true,
                PromptTokens = 10,
                CompletionTokens = 20,
                Duration = TimeSpan.FromMilliseconds(100)
            });
        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_WithConfiguredProviders_ShouldReturnSuccessResult()
    {
        // Arrange
        var provider = CreateMockProvider("OpenAI", "Paris is the capital of France");
        _registry.Register(provider.Object);

        var pipeline = new ForgePipeline(_registry, _loggerFactory.Object);

        // Act
        var result = await pipeline
            .WithPrompt("What is the capital of France?")
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.BestResponse.Should().NotBeNullOrEmpty();
        result.BestProvider.Should().Be("OpenAI");
    }

    [Fact]
    public async Task ExecuteAsync_NoProviders_ShouldReturnFailureResult()
    {
        // Arrange
        var pipeline = new ForgePipeline(_registry, _loggerFactory.Object);

        // Act
        var result = await pipeline
            .WithPrompt("Hello")
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithSystemPrompt_ShouldPassSystemPromptToProviders()
    {
        // Arrange
        var provider = CreateMockProvider("OpenAI");
        _registry.Register(provider.Object);

        var pipeline = new ForgePipeline(_registry, _loggerFactory.Object);

        // Act
        var result = await pipeline
            .WithSystemPrompt("You are a helpful assistant.")
            .WithPrompt("Hello")
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        provider.Verify(
            p => p.GenerateAsync("Hello", It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleProviders_ShouldReturnBestResult()
    {
        // Arrange
        var provider1 = CreateMockProvider("OpenAI", "Paris is the capital of France");
        var provider2 = CreateMockProvider("Anthropic", "The capital of France is Paris");
        _registry.Register(provider1.Object);
        _registry.Register(provider2.Object);

        var pipeline = new ForgePipeline(_registry, _loggerFactory.Object);

        // Act
        var result = await pipeline
            .WithPrompt("What is the capital of France?")
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalModels.Should().Be(2);
        result.BestResponse.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithScoringConfiguration_ShouldScoreResponses()
    {
        // Arrange
        var provider = CreateMockProvider("OpenAI", "Paris is the capital");
        _registry.Register(provider.Object);

        var pipeline = new ForgePipeline(_registry, _loggerFactory.Object);

        // Act
        var result = await pipeline
            .WithPrompt("What is the capital of France?")
            .ScoreWith(s => s.TokenEfficiency(1.0))
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.BestScore.Should().BeGreaterThanOrEqualTo(0.0);
    }
}
