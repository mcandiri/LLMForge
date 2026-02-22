using System.Net;
using LLMForge.Resilience;

namespace LLMForge.Providers;

/// <summary>
/// Exception thrown by LLM providers with rich status information for retry decisions.
/// </summary>
public class LLMProviderException : Exception
{
    /// <summary>
    /// Gets the HTTP status code returned by the provider, if available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets whether this error is retryable (e.g., 429, 500, 502, 503, 504).
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Gets whether this was a rate-limit (429) response.
    /// </summary>
    public bool IsRateLimited => StatusCode == HttpStatusCode.TooManyRequests;

    /// <summary>
    /// Gets parsed rate-limit information from response headers, if available.
    /// </summary>
    public RateLimitInfo? RateLimitInfo { get; }

    /// <summary>
    /// Gets the provider name that threw this exception.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="LLMProviderException"/>.
    /// </summary>
    public LLMProviderException(
        string providerName,
        HttpStatusCode statusCode,
        string message,
        RateLimitInfo? rateLimitInfo = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ProviderName = providerName;
        StatusCode = statusCode;
        RateLimitInfo = rateLimitInfo;
        IsRetryable = IsRetryableStatusCode(statusCode);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LLMProviderException"/> without an HTTP status code.
    /// </summary>
    public LLMProviderException(string providerName, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ProviderName = providerName;
        StatusCode = null;
        IsRetryable = false;
        RateLimitInfo = null;
    }

    private static bool IsRetryableStatusCode(HttpStatusCode code) => code is
        HttpStatusCode.TooManyRequests or       // 429
        HttpStatusCode.InternalServerError or    // 500
        HttpStatusCode.BadGateway or             // 502
        HttpStatusCode.ServiceUnavailable or     // 503
        HttpStatusCode.GatewayTimeout;           // 504
}
