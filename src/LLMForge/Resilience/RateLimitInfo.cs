using System.Net.Http.Headers;

namespace LLMForge.Resilience;

/// <summary>
/// Parsed rate-limit information from HTTP response headers.
/// </summary>
public class RateLimitInfo
{
    /// <summary>
    /// Gets or sets the recommended retry delay parsed from Retry-After header.
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>
    /// Gets or sets the remaining requests in the current window (X-RateLimit-Remaining).
    /// </summary>
    public int? RemainingRequests { get; set; }

    /// <summary>
    /// Gets or sets when the rate limit window resets (X-RateLimit-Reset).
    /// </summary>
    public DateTimeOffset? ResetAt { get; set; }

    /// <summary>
    /// Gets or sets the total request limit for the current window (X-RateLimit-Limit).
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Parses rate-limit information from HTTP response headers.
    /// </summary>
    public static RateLimitInfo FromHeaders(HttpResponseHeaders headers)
    {
        var info = new RateLimitInfo();

        // Parse Retry-After (seconds or HTTP-date)
        if (headers.RetryAfter != null)
        {
            if (headers.RetryAfter.Delta.HasValue)
            {
                info.RetryAfter = headers.RetryAfter.Delta.Value;
            }
            else if (headers.RetryAfter.Date.HasValue)
            {
                var delay = headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                info.RetryAfter = delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
            }
        }

        // Parse X-RateLimit-Remaining
        if (TryGetHeaderInt(headers, "X-RateLimit-Remaining", out var remaining))
        {
            info.RemainingRequests = remaining;
        }

        // Parse X-RateLimit-Limit
        if (TryGetHeaderInt(headers, "X-RateLimit-Limit", out var limit))
        {
            info.Limit = limit;
        }

        // Parse X-RateLimit-Reset (Unix timestamp)
        if (TryGetHeaderLong(headers, "X-RateLimit-Reset", out var resetEpoch))
        {
            info.ResetAt = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);
        }

        return info;
    }

    private static bool TryGetHeaderInt(HttpResponseHeaders headers, string name, out int value)
    {
        value = 0;
        if (!headers.TryGetValues(name, out var values)) return false;
        return int.TryParse(values.FirstOrDefault(), out value);
    }

    private static bool TryGetHeaderLong(HttpResponseHeaders headers, string name, out long value)
    {
        value = 0;
        if (!headers.TryGetValues(name, out var values)) return false;
        return long.TryParse(values.FirstOrDefault(), out value);
    }
}
