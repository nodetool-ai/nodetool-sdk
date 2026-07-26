using System.Net;

namespace Nodetool.SDK.Connection;

/// <summary>
/// Bounded retry policy for side-effect-free SDK discovery and preflight
/// requests. It must not be applied to workflow submission.
/// </summary>
public sealed record NodeToolReadRetryPolicy
{
    public static NodeToolReadRetryPolicy None { get; } =
        new() { MaximumAttempts = 1 };

    public static NodeToolReadRetryPolicy Default { get; } = new();

    public int MaximumAttempts { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } =
        TimeSpan.FromMilliseconds(150);
    public TimeSpan MaximumDelay { get; init; } =
        TimeSpan.FromSeconds(2);

    internal bool ShouldRetry(
        HttpStatusCode statusCode,
        int attempt)
        => attempt < MaximumAttempts &&
           statusCode is
               HttpStatusCode.RequestTimeout or
               HttpStatusCode.TooManyRequests or
               HttpStatusCode.BadGateway or
               HttpStatusCode.ServiceUnavailable or
               HttpStatusCode.GatewayTimeout;

    internal TimeSpan GetDelay(
        int attempt,
        TimeSpan? retryAfter = null)
    {
        var exponential = TimeSpan.FromMilliseconds(
            InitialDelay.TotalMilliseconds *
            Math.Pow(2, Math.Max(0, attempt - 1)));
        var selected = retryAfter is { } serverDelay &&
                       serverDelay > exponential
            ? serverDelay
            : exponential;
        return selected > MaximumDelay ? MaximumDelay : selected;
    }

    internal void Validate()
    {
        if (MaximumAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(MaximumAttempts));
        if (InitialDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(InitialDelay));
        if (MaximumDelay < InitialDelay)
            throw new ArgumentOutOfRangeException(nameof(MaximumDelay));
    }
}
