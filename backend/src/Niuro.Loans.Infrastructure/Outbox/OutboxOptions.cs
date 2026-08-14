namespace Niuro.Loans.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Turned off in the API integration tests, which exercise the request path only.
    /// The dispatcher has its own tests.
    /// </summary>
    public bool Enabled { get; init; } = true;

    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; init; } = 20;

    /// <summary>
    /// After this many failures a message stops being retried. It is left in the table,
    /// unprocessed and with its last error, rather than deleted: a human can see it.
    /// </summary>
    public int MaxAttempts { get; init; } = 5;

    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(5);

    public string BaseUrl { get; init; } = "http://localhost:4000";

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Doubles with each failed attempt, capped so it never backs off forever.</summary>
    public TimeSpan RetryDelayAfter(int attempts)
    {
        var seconds = InitialRetryDelay.TotalSeconds * Math.Pow(2, Math.Max(0, attempts - 1));
        return TimeSpan.FromSeconds(Math.Min(seconds, MaxRetryDelay.TotalSeconds));
    }
}
