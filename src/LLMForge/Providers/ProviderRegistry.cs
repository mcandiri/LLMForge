namespace LLMForge.Providers;

/// <summary>
/// Registry that manages all configured LLM providers.
/// </summary>
public class ProviderRegistry
{
    private readonly Dictionary<string, ILLMProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Registers a provider in the registry.
    /// </summary>
    /// <param name="provider">The provider to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    public void Register(ILLMProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_lock)
        {
            _providers[provider.Name] = provider;
        }
    }

    /// <summary>
    /// Gets a provider by name.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <returns>The provider if found; otherwise null.</returns>
    public ILLMProvider? GetProvider(string name)
    {
        lock (_lock)
        {
            return _providers.TryGetValue(name, out var provider) ? provider : null;
        }
    }

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <returns>A read-only collection of all providers.</returns>
    public IReadOnlyList<ILLMProvider> GetAll()
    {
        lock (_lock)
        {
            return _providers.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all configured (ready-to-use) providers.
    /// </summary>
    /// <returns>A read-only collection of configured providers.</returns>
    public IReadOnlyList<ILLMProvider> GetConfigured()
    {
        lock (_lock)
        {
            return _providers.Values.Where(p => p.IsConfigured).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets providers by name filter.
    /// </summary>
    /// <param name="names">The provider names to include.</param>
    /// <returns>Matching providers.</returns>
    public IReadOnlyList<ILLMProvider> GetByNames(params string[] names)
    {
        var nameSet = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        lock (_lock)
        {
            return _providers.Values
                .Where(p => nameSet.Contains(p.Name))
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the count of registered providers.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _providers.Count;
            }
        }
    }

    /// <summary>
    /// Checks if a provider with the given name is registered.
    /// </summary>
    public bool Contains(string name)
    {
        lock (_lock)
        {
            return _providers.ContainsKey(name);
        }
    }
}
