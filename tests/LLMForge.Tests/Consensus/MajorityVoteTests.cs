using FluentAssertions;
using LLMForge.Consensus;
using LLMForge.Scoring;
using Xunit;

namespace LLMForge.Tests.Consensus;

public class MajorityVoteTests
{
    private readonly MajorityVoteStrategy _sut = new(similarityThreshold: 0.6);

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
    public async Task EvaluateAsync_MajorityAgreement_ShouldReachConsensus()
    {
        // Arrange
        var responses = new List<ScoredResponse>
        {
            CreateScoredResponse("OpenAI", "The capital of France is Paris", 0.9),
            CreateScoredResponse("Anthropic", "Paris is the capital of France", 0.85),
            CreateScoredResponse("Gemini", "France capital is Paris city", 0.8)
        };

        // Act
        var result = await _sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeTrue();
        result.BestResponse.Should().NotBeNullOrEmpty();
        result.TotalModels.Should().Be(3);
        result.Confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task EvaluateAsync_NoAgreement_ShouldNotReachConsensus()
    {
        // Arrange
        var responses = new List<ScoredResponse>
        {
            CreateScoredResponse("OpenAI", "cats dogs animals pets fur"),
            CreateScoredResponse("Anthropic", "mathematics algebra geometry calculus equations"),
            CreateScoredResponse("Gemini", "programming software engineering code development")
        };

        // Act
        var result = await _sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeFalse();
        result.DissentingModels.Should().NotBeEmpty();
        result.TotalModels.Should().Be(3);
    }

    [Fact]
    public async Task EvaluateAsync_EmptyResponses_ShouldReturnNoConsensus()
    {
        // Arrange
        var responses = new List<ScoredResponse>();

        // Act
        var result = await _sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeFalse();
        result.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task EvaluateAsync_SingleResponse_ShouldAlwaysReachConsensus()
    {
        // Arrange
        var responses = new List<ScoredResponse>
        {
            CreateScoredResponse("OpenAI", "The only response here", 0.95)
        };

        // Act
        var result = await _sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeTrue();
        result.BestProvider.Should().Be("OpenAI");
        result.BestScore.Should().Be(0.95);
        result.Confidence.Should().Be(1.0);
        result.AgreementCount.Should().Be(1);
    }

    [Fact]
    public async Task EvaluateAsync_TwoAgreeOneDisagrees_ShouldPickHighestScoredInMajority()
    {
        // Arrange
        var responses = new List<ScoredResponse>
        {
            CreateScoredResponse("OpenAI", "The capital of France is Paris in Europe", 0.7),
            CreateScoredResponse("Anthropic", "Paris is the capital city of France in Europe", 0.9),
            CreateScoredResponse("Gemini", "quantum physics string theory dark matter energy", 0.6)
        };

        // Act
        var result = await _sut.EvaluateAsync(responses);

        // Assert
        result.ConsensusReached.Should().BeTrue();
        result.AgreementCount.Should().BeGreaterThanOrEqualTo(2);
        result.AllScoredResponses.Should().HaveCount(3);
    }
}
