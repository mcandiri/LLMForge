using FluentAssertions;
using LLMForge.Consensus;
using LLMForge.Scoring;
using Xunit;

namespace LLMForge.Tests.Consensus;

public class QuorumStrategyTests
{
    private static ScoredResponse CreateScoredResponse(string providerName, string content, double score = 0.8)
    {
        return new ScoredResponse
        {
            ProviderName = providerName,
            Content = content,
            Score = score,
            ResponseTime = TimeSpan.FromMilliseconds(100),
            TotalTokens = 50
        };
    }

    [Fact]
    public async Task EvaluateAsync_QuorumMet_ShouldReachConsensus()
    {
        // Arrange
        var sut = new QuorumStrategy(requiredCount: 2, similarityThreshold: 0.6);
        var responses = new List<ScoredResponse>
        {
            CreateScoredResponse("OpenAI", "The capital of France is Paris", 0.9),
            CreateScoredResponse("Anthropic", "Paris is the capital of France", 0.85),
            CreateScoredResponse("Gemini", "quantum physics dark matter energy", 0.7)
        };

        // Act
        var result = await sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeTrue();
        result.AgreementCount.Should().BeGreaterThanOrEqualTo(2);
        result.TotalModels.Should().Be(3);
    }

    [Fact]
    public async Task EvaluateAsync_QuorumNotMet_ShouldNotReachConsensus()
    {
        // Arrange
        var sut = new QuorumStrategy(requiredCount: 3, similarityThreshold: 0.6);
        var responses = new List<ScoredResponse>
        {
            CreateScoredResponse("OpenAI", "cats dogs animals pets fur"),
            CreateScoredResponse("Anthropic", "mathematics algebra geometry calculus equations"),
            CreateScoredResponse("Gemini", "programming software engineering code development")
        };

        // Act
        var result = await sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeFalse();
        result.TotalModels.Should().Be(3);
    }

    [Fact]
    public async Task EvaluateAsync_EmptyResponses_ShouldReturnNoConsensus()
    {
        // Arrange
        var sut = new QuorumStrategy(requiredCount: 2);
        var responses = new List<ScoredResponse>();

        // Act
        var result = await sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeFalse();
        result.Confidence.Should().Be(0);
    }

    [Fact]
    public void Constructor_ZeroRequiredCount_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = () => new QuorumStrategy(requiredCount: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task EvaluateAsync_ExactQuorumMet_ShouldReportDissentingModels()
    {
        // Arrange
        var sut = new QuorumStrategy(requiredCount: 2, similarityThreshold: 0.6);
        var responses = new List<ScoredResponse>
        {
            CreateScoredResponse("OpenAI", "The capital of France is Paris in Europe", 0.9),
            CreateScoredResponse("Anthropic", "Paris is the capital city of France in Europe", 0.85),
            CreateScoredResponse("Gemini", "quantum physics string theory dark matter energy", 0.7)
        };

        // Act
        var result = await sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeTrue();
        result.DissentingModels.Should().NotBeEmpty();
        result.Confidence.Should().BeGreaterThan(0);
        result.Confidence.Should().BeLessOrEqualTo(1.0);
    }
}
