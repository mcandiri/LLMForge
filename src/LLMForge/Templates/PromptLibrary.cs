using System.Collections.Concurrent;

namespace LLMForge.Templates;

/// <summary>
/// A thread-safe library for storing and retrieving reusable prompt templates.
/// </summary>
public class PromptLibrary
{
    private readonly ConcurrentDictionary<string, IPromptTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a template in the library.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <param name="template">The template.</param>
    public void Register(string name, IPromptTemplate template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(template);

        _templates[name] = template;
    }

    /// <summary>
    /// Gets a template by name.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <returns>The template if found; otherwise null.</returns>
    public IPromptTemplate? Get(string name)
    {
        return _templates.TryGetValue(name, out var template) ? template : null;
    }

    /// <summary>
    /// Gets all registered template names.
    /// </summary>
    public IReadOnlyList<string> GetNames()
    {
        return _templates.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Removes a template from the library.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <returns>True if the template was removed.</returns>
    public bool Remove(string name)
    {
        return _templates.TryRemove(name, out _);
    }

    /// <summary>
    /// Gets the count of registered templates.
    /// </summary>
    public int Count => _templates.Count;
}
