namespace LLMForge.Templates;

/// <summary>
/// Defines a prompt template with variable substitution support.
/// </summary>
public interface IPromptTemplate
{
    /// <summary>
    /// Gets the template name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the system prompt template.
    /// </summary>
    string? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets the user prompt template.
    /// </summary>
    string UserPrompt { get; set; }

    /// <summary>
    /// Renders the template with the given variables.
    /// </summary>
    /// <param name="variables">The variable values to substitute.</param>
    /// <returns>The rendered prompt pair (systemPrompt, userPrompt).</returns>
    (string? SystemPrompt, string UserPrompt) Render(IDictionary<string, string> variables);
}
