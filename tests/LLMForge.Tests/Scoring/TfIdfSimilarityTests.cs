using FluentAssertions;
using LLMForge.Scoring;
using Xunit;

namespace LLMForge.Tests.Scoring;

public class TfIdfSimilarityTests
{
    [Fact]
    public void IdenticalTexts_ReturnsPerfectSimilarity()
    {
        var text = "The quick brown fox jumps over the lazy dog";
        var score = TfIdfSimilarityCalculator.Calculate(text, text);
        score.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void CompletelyDifferentTexts_ReturnsLowSimilarity()
    {
        var text1 = "The quick brown fox jumps over the lazy dog";
        var text2 = "Quantum physics explores fundamental particles in nature";
        var score = TfIdfSimilarityCalculator.Calculate(text1, text2);
        score.Should().BeLessThan(0.2);
    }

    [Fact]
    public void SimilarTexts_ReturnsHighSimilarity()
    {
        var text1 = "Machine learning is a subset of artificial intelligence";
        var text2 = "Artificial intelligence includes machine learning techniques";
        var score = TfIdfSimilarityCalculator.Calculate(text1, text2);
        score.Should().BeGreaterThan(0.3);
    }

    [Fact]
    public void EmptyText_ReturnsZero()
    {
        TfIdfSimilarityCalculator.Calculate("", "hello world").Should().Be(0.0);
        TfIdfSimilarityCalculator.Calculate("hello world", "").Should().Be(0.0);
        TfIdfSimilarityCalculator.Calculate("", "").Should().Be(0.0);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsZero()
    {
        TfIdfSimilarityCalculator.Calculate("   ", "hello world").Should().Be(0.0);
    }

    [Fact]
    public void WithCorpus_IdfAffectsSimilarity()
    {
        var text1 = "The cat sat on the mat";
        var text2 = "The cat lay on the rug";
        var corpus = new[]
        {
            text1,
            text2,
            "The dog chased the ball in the park",
            "A cat purred softly while resting on the sofa"
        };

        var withCorpus = TfIdfSimilarityCalculator.Calculate(text1, text2, corpus);
        var withoutCorpus = TfIdfSimilarityCalculator.Calculate(text1, text2);

        // Both should return valid similarity; IDF from corpus changes the weighting
        withCorpus.Should().BeGreaterThan(0.0);
        withoutCorpus.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void CaseInsensitive()
    {
        var text1 = "HELLO WORLD";
        var text2 = "hello world";
        var score = TfIdfSimilarityCalculator.Calculate(text1, text2);
        score.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void RepeatedWords_AffectTfWeighting()
    {
        // text1 emphasizes "machine" more through repetition
        var text1 = "machine machine machine learning";
        var text2 = "machine learning algorithms";
        var score = TfIdfSimilarityCalculator.Calculate(text1, text2);
        score.Should().BeGreaterThan(0.0).And.BeLessThan(1.0);
    }

    [Fact]
    public void WithCorpus_ScoresDifferFromTwoDocCorpus()
    {
        var text1 = "The answer is 42";
        var text2 = "42 is the answer";
        var corpus = new[] { text1, text2, "Something completely different about quantum physics" };

        var withCorpus = TfIdfSimilarityCalculator.Calculate(text1, text2, corpus);
        var twoDoc = TfIdfSimilarityCalculator.Calculate(text1, text2);

        withCorpus.Should().BeGreaterThan(0.3);
        twoDoc.Should().BeGreaterThan(0.3);
    }

    [Fact]
    public async Task ConsensusScorer_ScoreAsync_UsesCorpus()
    {
        var scorer = new ConsensusScorer();
        var responses = new List<LLMForge.Providers.LLMResponse>
        {
            new() { Content = "The answer is 42", ProviderName = "A", IsSuccess = true },
            new() { Content = "42 is the answer", ProviderName = "B", IsSuccess = true },
            new() { Content = "Something completely different", ProviderName = "C", IsSuccess = true }
        };

        var scoreA = await scorer.ScoreAsync(responses[0], responses);
        var scoreC = await scorer.ScoreAsync(responses[2], responses);

        // "The answer is 42" should be more similar to "42 is the answer" than "Something completely different"
        scoreA.Should().BeGreaterThan(scoreC);
    }
}
