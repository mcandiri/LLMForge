using FluentAssertions;
using LLMForge.Validation;
using Xunit;

namespace LLMForge.Tests.Validation;

public class ContentFilterValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ContentContainsAllRequiredKeywords_ShouldReturnSuccess()
    {
        // Arrange
        var sut = new ContentFilterValidator(mustContain: new[] { "hello", "world" });

        // Act
        var result = await sut.ValidateAsync("hello beautiful world");

        // Assert
        result.IsValid.Should().BeTrue();
        result.ValidatorName.Should().Be("ContentFilter");
    }

    [Fact]
    public async Task ValidateAsync_ContentMissingRequiredKeyword_ShouldReturnFailure()
    {
        // Arrange
        var sut = new ContentFilterValidator(mustContain: new[] { "hello", "missing" });

        // Act
        var result = await sut.ValidateAsync("hello beautiful world");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("missing");
    }

    [Fact]
    public async Task ValidateAsync_ContentContainsForbiddenKeyword_ShouldReturnFailure()
    {
        // Arrange
        var sut = new ContentFilterValidator(mustNotContain: new[] { "forbidden", "banned" });

        // Act
        var result = await sut.ValidateAsync("This contains a forbidden word");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("forbidden");
    }

    [Fact]
    public async Task ValidateAsync_EmptyContent_ShouldReturnFailure()
    {
        // Arrange
        var sut = new ContentFilterValidator(mustContain: new[] { "hello" });

        // Act
        var result = await sut.ValidateAsync("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task ValidateAsync_CaseInsensitiveByDefault_ShouldMatchRegardlessOfCase()
    {
        // Arrange
        var sut = new ContentFilterValidator(
            mustContain: new[] { "HELLO" },
            mustNotContain: new[] { "FORBIDDEN" });

        // Act
        var resultPass = await sut.ValidateAsync("hello world");
        var resultFail = await sut.ValidateAsync("hello forbidden zone");

        // Assert
        resultPass.IsValid.Should().BeTrue();
        resultFail.IsValid.Should().BeFalse();
    }
}
