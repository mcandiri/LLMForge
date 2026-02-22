using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using LLMForge.Resilience;
using Xunit;

namespace LLMForge.Tests.Resilience;

public class RateLimitInfoTests
{
    [Fact]
    public void FromHeaders_ParsesRetryAfterSeconds()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        var info = RateLimitInfo.FromHeaders(response.Headers);

        info.RetryAfter.Should().NotBeNull();
        info.RetryAfter!.Value.TotalSeconds.Should().BeApproximately(30, 1);
    }

    [Fact]
    public void FromHeaders_ParsesRetryAfterDate()
    {
        var futureDate = DateTimeOffset.UtcNow.AddSeconds(60);
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(futureDate);

        var info = RateLimitInfo.FromHeaders(response.Headers);

        info.RetryAfter.Should().NotBeNull();
        info.RetryAfter!.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FromHeaders_ParsesRateLimitHeaders()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.Add("X-RateLimit-Remaining", "5");
        response.Headers.Add("X-RateLimit-Limit", "100");

        var epochReset = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
        response.Headers.Add("X-RateLimit-Reset", epochReset.ToString());

        var info = RateLimitInfo.FromHeaders(response.Headers);

        info.RemainingRequests.Should().Be(5);
        info.Limit.Should().Be(100);
        info.ResetAt.Should().NotBeNull();
    }

    [Fact]
    public void FromHeaders_HandlesEmptyHeaders()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var info = RateLimitInfo.FromHeaders(response.Headers);

        info.RetryAfter.Should().BeNull();
        info.RemainingRequests.Should().BeNull();
        info.Limit.Should().BeNull();
        info.ResetAt.Should().BeNull();
    }

    [Fact]
    public void FromHeaders_HandlesMalformedValues()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.Add("X-RateLimit-Remaining", "not-a-number");

        var info = RateLimitInfo.FromHeaders(response.Headers);

        info.RemainingRequests.Should().BeNull();
    }
}
