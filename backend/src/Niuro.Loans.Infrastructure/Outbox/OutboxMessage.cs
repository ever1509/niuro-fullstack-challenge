namespace Niuro.Loans.Infrastructure.Outbox;

/// <summary>
/// An event waiting to be delivered, stored in the same database as the data that produced it.
/// <para>
/// That is the whole point: the row is written by the same transaction that writes the customer
/// and the application, so a rollback takes the event with it. Nothing can be delivered for
/// work that was never committed, and nothing committed can be silently forgotten.
/// </para>
/// </summary>
public sealed class OutboxMessage
{
    private OutboxMessage(Guid id, string type, string payload, DateTime occurredAtUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        NextAttemptAtUtc = occurredAtUtc;
    }

    // Required by EF Core to materialise the entity.
    private OutboxMessage()
    {
        Type = null!;
        Payload = null!;
    }

    public Guid Id { get; private init; }

    /// <summary>Which kind of event this is, so the dispatcher knows how to read the payload.</summary>
    public string Type { get; private init; }

    public string Payload { get; private init; }

    public DateTime OccurredAtUtc { get; private init; }

    /// <summary>Set once delivery has succeeded. A null value means still outstanding.</summary>
    public DateTime? ProcessedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    /// <summary>When this message becomes eligible for another attempt.</summary>
    public DateTime NextAttemptAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage For(string type, string payload, DateTime utcNow) =>
        new(Guid.NewGuid(), type, payload, utcNow);

    public void MarkDelivered(DateTime utcNow)
    {
        Attempts++;
        ProcessedAtUtc = utcNow;
        LastError = null;
    }

    /// <summary>
    /// Records a failed attempt and backs off before the next one, so a service that is down
    /// is not hammered once per poll.
    /// </summary>
    public void MarkFailed(string error, DateTime utcNow, TimeSpan retryDelay)
    {
        Attempts++;
        LastError = error;
        NextAttemptAtUtc = utcNow + retryDelay;
    }
}
