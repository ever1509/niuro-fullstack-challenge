using System.Text.Json;
using Niuro.Loans.Application.Events;
using Niuro.Loans.Infrastructure.Persistence;

namespace Niuro.Loans.Infrastructure.Outbox;

/// <summary>
/// Turns a published event into a row in the outbox table.
/// <para>
/// It uses the same <see cref="LoansDbContext"/> as the repositories, which is what makes the
/// event part of the caller's transaction rather than a separate action that could succeed
/// or fail on its own.
/// </para>
/// </summary>
internal sealed class OutboxEventPublisher(LoansDbContext dbContext, TimeProvider timeProvider) : IEventPublisher
{
    public void Publish(LoanApplicationRecorded @event)
    {
        var message = OutboxMessage.For(
            nameof(LoanApplicationRecorded),
            JsonSerializer.Serialize(@event),
            timeProvider.GetUtcNow().UtcDateTime);

        dbContext.OutboxMessages.Add(message);
    }
}
