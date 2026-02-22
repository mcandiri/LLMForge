using FluentAssertions;
using LLMForge.Configuration;
using LLMForge.Execution;
using LLMForge.Providers;
using LLMForge.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LLMForge.Tests.Execution;

public class FallbackExecutionTests
{
    private static FallbackExecutionStrategy CreateStrategy(
        FallbackTrigger triggers = FallbackTrigger.All,
        IReadOnlyList<IResponseValidator>? validators = null)
    {
        var logger = new Mock<ILogger<FallbackExecutionStrategy>>();
        return new FallbackExecutionStrategy(logger.Object, triggers, validators);
    }

    private static Mock<ILLMProvider> CreateSuccessProvider(string name, string content = "valid response")
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

    private static Mock<ILLMProvider> CreateFailureProvider(string name, string error = "API Error")
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
    public async Task ExecuteAsync_FirstProviderSucceeds_ShouldReturnWithoutFallback()
    {
        // Arrange
        var sut = CreateStrategy();
        var provider1 = CreateSuccessProvider("OpenAI");
        var provider2 = CreateSuccessProvider("Anthropic");
        var providers = new List<ILLMProvider> { provider1.Object, provider2.Object };

        // Act
        var result = await sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(1);
        result.Responses.Should().ContainKey("OpenAI");
        result.HasSuccessfulResponse.Should().BeTrue();
        provider2.Verify(
            p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FirstProviderFailsWithException_ShouldFallbackToNext()
    {
        // Arrange
        var sut = CreateStrategy(FallbackTrigger.Exception);
        var provider1 = CreateFailureProvider("OpenAI");
        var provider2 = CreateSuccessProvider("Anthropic");
        var providers = new List<ILLMProvider> { provider1.Object, provider2.Object };

        // Act
        var result = await sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(2);
        result.HasSuccessfulResponse.Should().BeTrue();
        result.SuccessfulResponses.Should().ContainSingle()
            .Which.ProviderName.Should().Be("Anthropic");
    }

    [Fact]
    public async Task ExecuteAsync_ValidationFailureTrigger_ShouldFallbackOnInvalidResponse()
    {
        // Arrange
        var mockValidator = new Mock<IResponseValidator>();
        mockValidator.Setup(v => v.ValidateAsync("invalid json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("JsonSchema", "Invalid JSON"));
        mockValidator.Setup(v => v.ValidateAsync("valid response", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success("JsonSchema"));

        var sut = CreateStrategy(
            FallbackTrigger.All,
            new List<IResponseValidator> { mockValidator.Object });

        var provider1 = CreateSuccessProvider("OpenAI", "invalid json");
        var provider2 = CreateSuccessProvider("Anthropic", "valid response");
        var providers = new List<ILLMProvider> { provider1.Object, provider2.Object };

        // Act
        var result = await sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(2);
        result.HasSuccessfulResponse.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AllProvidersFail_ShouldReturnAllFailedResponses()
    {
        // Arrange
        var sut = CreateStrategy();
        var providers = new List<ILLMProvider>
        {
            CreateFailureProvider("OpenAI").Object,
            CreateFailureProvider("Anthropic").Object,
            CreateFailureProvider("Gemini").Object
        };

        // Act
        var result = await sut.ExecuteAsync(providers, "Hello");

        // Assert
        result.Responses.Should().HaveCount(3);
        result.HasSuccessfulResponse.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_EmptyProviders_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateStrategy();
        var providers = new List<ILLMProvider>();

        // Act
        var act = () => sut.ExecuteAsync(providers, "Hello");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one provider*");
    }
}
