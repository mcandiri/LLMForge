using FluentAssertions;
using LLMForge.Providers;
using Moq;
using Xunit;

namespace LLMForge.Tests.Providers;

public class ProviderRegistryTests
{
    private readonly ProviderRegistry _sut = new();

    private Mock<ILLMProvider> CreateMockProvider(string name, bool isConfigured = true)
    {
        var mock = new Mock<ILLMProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.IsConfigured).Returns(isConfigured);
        mock.Setup(p => p.ModelId).Returns($"{name}-model");
        mock.Setup(p => p.DisplayName).Returns($"{name}/{name}-model");
        return mock;
    }

    [Fact]
    public void Register_SingleProvider_ShouldBeRetrievableByName()
    {
        // Arrange
        var provider = CreateMockProvider("OpenAI").Object;

        // Act
        _sut.Register(provider);
        var result = _sut.GetProvider("OpenAI");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("OpenAI");
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public void GetProvider_NonExistentName_ShouldReturnNull()
    {
        // Arrange
        _sut.Register(CreateMockProvider("OpenAI").Object);

        // Act
        var result = _sut.GetProvider("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_MultipleProviders_ShouldReturnAllRegistered()
    {
        // Arrange
        _sut.Register(CreateMockProvider("OpenAI").Object);
        _sut.Register(CreateMockProvider("Anthropic").Object);
        _sut.Register(CreateMockProvider("Gemini").Object);

        // Act
        var all = _sut.GetAll();

        // Assert
        all.Should().HaveCount(3);
        all.Select(p => p.Name).Should().Contain(new[] { "OpenAI", "Anthropic", "Gemini" });
    }

    [Fact]
    public void GetConfigured_MixedProviders_ShouldReturnOnlyConfigured()
    {
        // Arrange
        _sut.Register(CreateMockProvider("OpenAI", isConfigured: true).Object);
        _sut.Register(CreateMockProvider("Anthropic", isConfigured: false).Object);
        _sut.Register(CreateMockProvider("Gemini", isConfigured: true).Object);

        // Act
        var configured = _sut.GetConfigured();

        // Assert
        configured.Should().HaveCount(2);
        configured.Select(p => p.Name).Should().Contain(new[] { "OpenAI", "Gemini" });
        configured.Select(p => p.Name).Should().NotContain("Anthropic");
    }

    [Fact]
    public void Contains_RegisteredProvider_ShouldReturnTrueCaseInsensitive()
    {
        // Arrange
        _sut.Register(CreateMockProvider("OpenAI").Object);

        // Act & Assert
        _sut.Contains("OpenAI").Should().BeTrue();
        _sut.Contains("openai").Should().BeTrue();
        _sut.Contains("OPENAI").Should().BeTrue();
        _sut.Contains("Unknown").Should().BeFalse();
    }
}
