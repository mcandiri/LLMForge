using FluentAssertions;
using LLMForge.Execution;
using LLMForge.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMForge.Tests.Execution;

public class SequentialExecutionTests
{
    private readonly SequentialExecutionStrategy _sut;

    public SequentialExecutionTests()
    {
        var logger = new Mock<ILogger<SequentialExecutionStrategy>>();
        _sut = new SequentialExecutionStrategy(logger.Object);
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
    public async Task ExecuteAsync_FirstProviderSucceeds_ShouldStopAndReturnOneResponse()
    {
        // Arrange
        var provider1 = CreateSuccessProvider("OpenAI");
        var provider2 = CreateSuccessProvider("Anthropic");
        var providers = new List<ILLMProvider> { provider1.Object, provider2.Object };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(1);
        result.Responses.Should().ContainKey("OpenAI");
        result.HasSuccessfulResponse.Should().BeTrue();
        provider2.Verify(
            p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FirstFailsSecondSucceeds_ShouldReturnTwoResponses()
    {
        // Arrange
        var provider1 = CreateFailureProvider("OpenAI");
        var provider2 = CreateSuccessProvider("Anthropic");
        var providers = new List<ILLMProvider> { provider1.Object, provider2.Object };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(2);
        result.HasSuccessfulResponse.Should().BeTrue();
        result.SuccessfulResponses.Should().HaveCount(1);
        result.SuccessfulResponses[0].ProviderName.Should().Be("Anthropic");
    }

    [Fact]
    public async Task ExecuteAsync_AllProvidersFail_ShouldReturnAllFailedResponses()
    {
        // Arrange
        var providers = new List<ILLMProvider>
        {
            CreateFailureProvider("OpenAI").Object,
            CreateFailureProvider("Anthropic").Object,
            CreateFailureProvider("Gemini").Object
        };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(3);
        result.HasSuccessfulResponse.Should().BeFalse();
        result.FailedResponses.Should().HaveCount(3);
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
    public async Task ExecuteAsync_MiddleProviderSucceeds_ShouldNotCallRemainingProviders()
    {
        // Arrange
        var provider1 = CreateFailureProvider("OpenAI");
        var provider2 = CreateSuccessProvider("Anthropic");
        var provider3 = CreateSuccessProvider("Gemini");
        var providers = new List<ILLMProvider>
        {
            provider1.Object, provider2.Object, provider3.Object
        };

        // Act
        var result = await _sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(2);
        provider1.Verify(
            p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        provider2.Verify(
            p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        provider3.Verify(
            p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
