namespace LLMForge.Scoring;

/// <summary>
/// Calculates text similarity using TF-IDF vectors and cosine similarity.
/// Self-contained — no external embedding APIs required.
/// </summary>
public static class TfIdfSimilarityCalculator
{
    private static readonly char[] Separators =
        { ' ', '\t', '\n', '\r', ',', '.', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'' };

    /// <summary>
    /// Computes cosine similarity between two texts using TF-IDF weighting
    /// over a corpus of all texts being compared (for IDF computation).
    /// </summary>
    /// <param name="text1">First text.</param>
    /// <param name="text2">Second text.</param>
    /// <param name="corpus">All texts in the comparison set (used for IDF). Must include text1 and text2.</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    public static double Calculate(string text1, string text2, IReadOnlyList<string> corpus)
    {
        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0.0;

        var tokens1 = Tokenize(text1);
        var tokens2 = Tokenize(text2);

        if (tokens1.Count == 0 || tokens2.Count == 0)
            return 0.0;

        // Build corpus token sets for IDF (just unique terms per document)
        var corpusTokenSets = corpus
            .Select(doc => new HashSet<string>(Tokenize(doc).Keys, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Collect all unique terms from both documents
        var allTerms = new HashSet<string>(tokens1.Keys, StringComparer.OrdinalIgnoreCase);
        allTerms.UnionWith(tokens2.Keys);

        // Compute TF-IDF vectors
        var vector1 = new double[allTerms.Count];
        var vector2 = new double[allTerms.Count];
        var i = 0;

        foreach (var term in allTerms)
        {
            var idf = ComputeIdf(term, corpusTokenSets);
            vector1[i] = ComputeTf(term, tokens1) * idf;
            vector2[i] = ComputeTf(term, tokens2) * idf;
            i++;
        }

        return CosineSimilarity(vector1, vector2);
    }

    /// <summary>
    /// Computes cosine similarity between two texts using TF-IDF weighting.
    /// Convenience overload that treats the two texts as the full corpus.
    /// </summary>
    public static double Calculate(string text1, string text2)
    {
        return Calculate(text1, text2, new[] { text1, text2 });
    }

    private static Dictionary<string, int> Tokenize(string text)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tokens = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in tokens)
        {
            var token = raw.ToLowerInvariant();
            if (token.Length <= 1) continue;

            if (freq.TryGetValue(token, out var count))
                freq[token] = count + 1;
            else
                freq[token] = 1;
        }

        return freq;
    }

    private static double ComputeTf(string term, Dictionary<string, int> termFrequencies)
    {
        if (!termFrequencies.TryGetValue(term, out var count))
            return 0.0;

        // Log-normalized TF: 1 + log(count)
        return 1.0 + Math.Log(count);
    }

    private static double ComputeIdf(string term, IReadOnlyList<HashSet<string>> corpusTokenSets)
    {
        var documentCount = corpusTokenSets.Count;
        var containingDocs = 0;

        foreach (var tokenSet in corpusTokenSets)
        {
            if (tokenSet.Contains(term))
                containingDocs++;
        }

        if (containingDocs == 0)
            return 0.0;

        // Standard IDF: log(N / df) + 1 to avoid zero for terms appearing in all docs
        return Math.Log((double)documentCount / containingDocs) + 1.0;
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        var dotProduct = 0.0;
        var magnitudeA = 0.0;
        var magnitudeB = 0.0;

        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA < 1e-10 || magnitudeB < 1e-10)
            return 0.0;

        return dotProduct / (magnitudeA * magnitudeB);
    }
}
