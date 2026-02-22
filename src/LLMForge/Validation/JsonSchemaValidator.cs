using System.Text.Json;

namespace LLMForge.Validation;

/// <summary>
/// Validates that the response is valid JSON and optionally contains required properties.
/// </summary>
public class JsonSchemaValidator : IResponseValidator
{
    private readonly string[]? _requiredProperties;

    /// <inheritdoc />
    public string Name => "JsonSchema";

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaValidator"/>.
    /// </summary>
    /// <param name="requiredProperties">Optional list of required top-level JSON property names.</param>
    public JsonSchemaValidator(string[]? requiredProperties = null)
    {
        _requiredProperties = requiredProperties;
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(ValidationResult.Failure(Name, "Response is empty"));
        }

        // Try to extract JSON from markdown code blocks
        var jsonContent = ExtractJson(content);

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);

            if (_requiredProperties is { Length: > 0 })
            {
                var root = doc.RootElement;
                var missingProperties = _requiredProperties
                    .Where(prop => !root.TryGetProperty(prop, out _))
                    .ToList();

                if (missingProperties.Count > 0)
                {
                    return Task.FromResult(ValidationResult.Failure(Name,
                        $"Missing required properties: {string.Join(", ", missingProperties)}"));
                }
            }

            return Task.FromResult(ValidationResult.Success(Name));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(ValidationResult.Failure(Name, $"Invalid JSON: {ex.Message}"));
        }
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();

        // Extract from ```json ... ``` blocks
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                var endBlock = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (endBlock > firstNewline)
                {
                    return trimmed[(firstNewline + 1)..endBlock].Trim();
                }
            }
        }

        return trimmed;
    }
}
