using FluentAssertions;
using LLMForge.Execution;
using LLMForge.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMForge.Tests.Execution;

public class ParallelExecutionTests
{
    private readonly ParallelExecutionStrategy _sut;

    public ParallelExecutionTests()
    {
        var logger = new Mock<ILogger<ParallelExecutionStrategy>>();
        _sut = new ParallelExecutionStrategy(logger.Object);
    }

    private Mock<ILLMProvider> CreateSuccessProvider(string name, string content = "response")
    {
        var mock = new Mock<ILLMProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.DisplayName).Returns($"{name}/model");
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

    private Mock<ILLMProvider> CreateFailureProvider(string name, string error = "API Error")
    {
        var mock = new Mock<ILLMProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.DisplayName).Returns($"{name}/model");
        mock.Setup(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse
            {
                ProviderName = name,
                ModelId = "model",
                IsSuccess = false,
                Error = error,
                Duration = TimeSpan.FromMilliseconds(50)
            });
        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_AllProvidersSucceed_ShouldReturnAllResponses()
    {
        // Arrange
        var providers = new List<ILLMProvider>
        {
            CreateSuccessProvider("OpenAI", "Hello from OpenAI").Object,
            CreateSuccessProvider("Anthropic", "Hello from Anthropic").Object
        };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(2);
        result.HasSuccessfulResponse.Should().BeTrue();
        result.SuccessfulResponses.Should().HaveCount(2);
        result.FailedResponses.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_SomeProvidersFail_ShouldIncludeBothSuccessesAndFailures()
    {
        // Arrange
        var providers = new List<ILLMProvider>
        {
            CreateSuccessProvider("OpenAI").Object,
            CreateFailureProvider("Anthropic").Object
        };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(2);
        result.HasSuccessfulResponse.Should().BeTrue();
        result.SuccessfulResponses.Should().HaveCount(1);
        result.FailedResponses.Should().HaveCount(1);
        result.FailedResponses[0].ProviderName.Should().Be("Anthropic");
    }

    [Fact]
    public async Task ExecuteAsync_AllProvidersFail_ShouldHaveNoSuccessfulResponse()
    {
        // Arrange
        var providers = new List<ILLMProvider>
        {
            CreateFailureProvider("OpenAI").Object,
            CreateFailureProvider("Anthropic").Object
        };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.HasSuccessfulResponse.Should().BeFalse();
        result.SuccessfulResponses.Should().BeEmpty();
        result.FailedResponses.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyProviders_ShouldThrowArgumentException()
    {
        // Arrange
        var providers = new List<ILLMProvider>();

        // Act
        var act = () => _sut.ExecuteAsync(providers, "Hello");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one provider*");
    }

    [Fact]
    public async Task ExecuteAsync_MultipleProviders_ShouldCallAllProvidersInParallel()
    {
        // Arrange
        var provider1 = CreateSuccessProvider("OpenAI");
        var provider2 = CreateSuccessProvider("Anthropic");
        var provider3 = CreateSuccessProvider("Gemini");
        var providers = new List<ILLMProvider>
        {
            provider1.Object, provider2.Object, provider3.Object
        };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello", "System prompt");

        // Assert
        provider1.Verify(p => p.GenerateAsync("Hello", "System prompt", It.IsAny<CancellationToken>()), Times.Once);
        provider2.Verify(p => p.GenerateAsync("Hello", "System prompt", It.IsAny<CancellationToken>()), Times.Once);
        provider3.Verify(p => p.GenerateAsync("Hello", "System prompt", It.IsAny<CancellationToken>()), Times.Once);
        result.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
