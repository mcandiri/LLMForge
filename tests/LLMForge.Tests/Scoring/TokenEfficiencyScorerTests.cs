using FluentAssertions;
using LLMForge.Providers;
using LLMForge.Scoring;
using Xunit;

namespace LLMForge.Tests.Scoring;

public class TokenEfficiencyScorerTests
{
    private readonly TokenEfficiencyScorer _sut = new();

    private static LLMResponse CreateResponse(string providerName, int completionTokens, bool isSuccess = true)
    {
        return new LLMResponse
        {
            ProviderName = providerName,
            ModelId = "model",
            Content = "response",
            IsSuccess = isSuccess,
            CompletionTokens = completionTokens,
            PromptTokens = 10,
            Duration = TimeSpan.FromMilliseconds(100)
        };
    }

    [Fact]
    public async Task ScoreAsync_FewestTokens_ShouldReturnHighestScore()
    {
        // Arrange
        var fewestTokens = CreateResponse("OpenAI", 50);
        var allResponses = new List<LLMResponse>
        {
            fewestTokens,
            CreateResponse("Anthropic", 100),
            CreateResponse("Gemini", 200)
        };

        // Act
        var score = await _sut.ScoreAsync(fewestTokens, allResponses);

        // Assert
        score.Should().Be(1.0);
    }

    [Fact]
    public async Task ScoreAsync_MostTokens_ShouldReturnLowestScore()
    {
        // Arrange
        var mostTokens = CreateResponse("Gemini", 200);
        var allResponses = new List<LLMResponse>
        {
            CreateResponse("OpenAI", 50),
            CreateResponse("Anthropic", 100),
            mostTokens
        };

        // Act
        var score = await _sut.ScoreAsync(mostTokens, allResponses);

        // Assert
        score.Should().Be(0.0);
    }

    [Fact]
    public async Task ScoreAsync_SingleResponse_ShouldReturnPerfectScore()
    {
        // Arrange
        var response = CreateResponse("OpenAI", 100);
        var allResponses = new List<LLMResponse> { response };

        // Act
        var score = await _sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().Be(1.0);
    }

    [Fact]
    public async Task ScoreAsync_AllSameTokenCount_ShouldReturnPerfectScore()
    {
        // Arrange
        var response = CreateResponse("OpenAI", 100);
        var allResponses = new List<LLMResponse>
        {
            response,
            CreateResponse("Anthropic", 100),
            CreateResponse("Gemini", 100)
        };

        // Act
        var score = await _sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().Be(1.0);
    }

    [Fact]
    public async Task ScoreAsync_MiddleTokenCount_ShouldReturnMiddleScore()
    {
        // Arrange
        var middleResponse = CreateResponse("Anthropic", 100);
        var allResponses = new List<LLMResponse>
        {
            CreateResponse("OpenAI", 50),
            middleResponse,
            CreateResponse("Gemini", 150)
        };

        // Act
        var score = await _sut.ScoreAsync(middleResponse, allResponses);

        // Assert
        // (100 - 50) / (150 - 50) = 0.5, inverted = 1 - 0.5 = 0.5
        score.Should().BeApproximately(0.5, 0.001);
    }
}
