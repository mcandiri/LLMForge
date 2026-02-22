using System.Text.RegularExpressions;

namespace LLMForge.Templates;

/// <summary>
/// A prompt template that supports {{variable}} substitution.
/// </summary>
public partial class PromptTemplate : IPromptTemplate
{
    private static readonly Regex VariablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? SystemPrompt { get; set; }

    /// <inheritdoc />
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default variable values.
    /// </summary>
    public Dictionary<string, string> Defaults { get; set; } = new();

    /// <inheritdoc />
    public (string? SystemPrompt, string UserPrompt) Render(IDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        // Merge defaults with provided variables (provided takes precedence)
        var merged = new Dictionary<string, string>(Defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in variables)
        {
            merged[kvp.Key] = kvp.Value;
        }

        var renderedSystem = SystemPrompt != null ? SubstituteVariables(SystemPrompt, merged) : null;
        var renderedUser = SubstituteVariables(UserPrompt, merged);

        return (renderedSystem, renderedUser);
    }

    private static string SubstituteVariables(string template, IDictionary<string, string> variables)
    {
        return VariablePattern.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            return variables.TryGetValue(variableName, out var value) ? value : match.Value;
        });
    }
}
