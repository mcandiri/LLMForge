using FluentAssertions;
using LLMForge.Providers;
using LLMForge.Scoring;
using Xunit;

namespace LLMForge.Tests.Scoring;

public class ConsensusScorerTests
{
    private readonly ConsensusScorer _sut = new();

    private static LLMResponse CreateResponse(string providerName, string content)
    {
        return new LLMResponse
        {
            ProviderName = providerName,
            ModelId = "model",
            Content = content,
            IsSuccess = true,
            Duration = TimeSpan.FromMilliseconds(100)
        };
    }

    [Fact]
    public async Task ScoreAsync_IdenticalResponses_ShouldReturnHighSimilarityScore()
    {
        // Arrange
        var response = CreateResponse("OpenAI", "The capital of France is Paris");
        var allResponses = new List<LLMResponse>
        {
            response,
            CreateResponse("Anthropic", "The capital of France is Paris"),
            CreateResponse("Gemini", "The capital of France is Paris")
        };

        // Act
        var score = await _sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().Be(1.0);
    }

    [Fact]
    public async Task ScoreAsync_CompletelyDifferentResponses_ShouldReturnLowScore()
    {
        // Arrange
        var response = CreateResponse("OpenAI", "cats dogs animals pets");
        var allResponses = new List<LLMResponse>
        {
            response,
            CreateResponse("Anthropic", "mathematics algebra geometry calculus"),
            CreateResponse("Gemini", "programming software engineering code")
        };

        // Act
        var score = await _sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().BeLessThan(0.3);
    }

    [Fact]
    public async Task ScoreAsync_SingleResponse_ShouldReturnPerfectScore()
    {
        // Arrange
        var response = CreateResponse("OpenAI", "Only one response here");
        var allResponses = new List<LLMResponse> { response };

        // Act
        var score = await _sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().Be(1.0);
    }

    [Fact]
    public async Task ScoreAsync_PartiallySimilarResponses_ShouldReturnMiddleScore()
    {
        // Arrange
        var response = CreateResponse("OpenAI", "Paris is the capital city of France in Europe");
        var allResponses = new List<LLMResponse>
        {
            response,
            CreateResponse("Anthropic", "The capital of France is Paris, located in Europe"),
            CreateResponse("Gemini", "France has its capital as Paris in western Europe")
        };

        // Act
        var score = await _sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().BeGreaterThan(0.3);
        score.Should().BeLessOrEqualTo(1.0);
    }

    [Fact]
    public async Task ScoreAsync_EmptyContentResponse_ShouldReturnLowScore()
    {
        // Arrange
        var response = CreateResponse("OpenAI", "");
        var allResponses = new List<LLMResponse>
        {
            response,
            CreateResponse("Anthropic", "This is a meaningful response with words")
        };

        // Act
        var score = await _sut.ScoreAsync(response, allResponses);

        // Assert
        score.Should().Be(0.0);
    }
}
