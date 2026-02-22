using FluentAssertions;
using LLMForge.Providers;
using LLMForge.Scoring;
using Moq;
using Xunit;

namespace LLMForge.Tests.Scoring;

public class WeightedScorerTests
{
    private static LLMResponse CreateResponse(string providerName = "TestProvider")
    {
        return new LLMResponse
        {
            ProviderName = providerName,
            ModelId = "model",
            Content = "test response",
            IsSuccess = true,
            Duration = TimeSpan.FromMilliseconds(100),
            CompletionTokens = 50,
            PromptTokens = 20
        };
    }

    private static Mock<IResponseScorer> CreateMockScorer(string name, double score)
    {
        var mock = new Mock<IResponseScorer>();
        mock.Setup(s => s.Name).Returns(name);
        mock.Setup(s => s.ScoreAsync(
                It.IsAny<LLMResponse>(),
                It.IsAny<IReadOnlyList<LLMResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        return mock;
    }

    [Fact]
    public async Task ScoreAsync_EqualWeights_ShouldReturnAverageOfScores()
    {
        // Arrange
        var sut = new WeightedScorer();
        sut.Add(CreateMockScorer("Scorer1", 0.8).Object, 1.0);
        sut.Add(CreateMockScorer("Scorer2", 0.6).Object, 1.0);

        var response = CreateResponse();
        var allResponses = new List<LLMResponse> { response };

        // Act
        var score = await sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().BeApproximately(0.7, 0.001);
    }

    [Fact]
    public async Task ScoreAsync_DifferentWeights_ShouldReturnWeightedAverage()
    {
        // Arrange
        var sut = new WeightedScorer();
        sut.Add(CreateMockScorer("Scorer1", 1.0).Object, 3.0);
        sut.Add(CreateMockScorer("Scorer2", 0.0).Object, 1.0);

        var response = CreateResponse();
        var allResponses = new List<LLMResponse> { response };

        // Act
        var score = await sut.ScoreAsync(response, allResponses);

        // Assert
        // Weighted: (1.0 * 3.0 + 0.0 * 1.0) / (3.0 + 1.0) = 0.75
        score.Should().BeApproximately(0.75, 0.001);
    }

    [Fact]
    public async Task ScoreAsync_NoScorers_ShouldReturnZero()
    {
        // Arrange
        var sut = new WeightedScorer();
        var response = CreateResponse();
        var allResponses = new List<LLMResponse> { response };

        // Act
        var score = await sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().Be(0.0);
    }

    [Fact]
    public async Task ScoreDetailedAsync_MultipleScorers_ShouldReturnBreakdownPerScorer()
    {
        // Arrange
        var sut = new WeightedScorer();
        sut.Add(CreateMockScorer("Consensus", 0.9).Object, 2.0);
        sut.Add(CreateMockScorer("TokenEfficiency", 0.7).Object, 1.0);

        var response = CreateResponse();
        var allResponses = new List<LLMResponse> { response };

        // Act
        var result = await sut.ScoreDetailedAsync(response, allResponses);

        // Assert
        result.ProviderName.Should().Be("TestProvider");
        result.ScoreBreakdown.Should().ContainKey("Consensus").WhoseValue.Should().BeApproximately(0.9, 0.001);
        result.ScoreBreakdown.Should().ContainKey("TokenEfficiency").WhoseValue.Should().BeApproximately(0.7, 0.001);
        // Weighted: (0.9 * 2.0 + 0.7 * 1.0) / (2.0 + 1.0) = 2.5 / 3.0 = 0.8333
        result.Score.Should().BeApproximately(0.8333, 0.001);
    }

    [Fact]
    public void Add_NegativeWeight_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var sut = new WeightedScorer();
        var scorer = CreateMockScorer("Test", 0.5).Object;

        // Act
        var act = () => sut.Add(scorer, -1.0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
