using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Niuro.Loans.Application.Events;
using Niuro.Loans.Domain.Customers;
using Niuro.Loans.Infrastructure.Persistence;

namespace Niuro.Loans.IntegrationTests;

/// <summary>
/// The requirement is that saving the customer, saving the application and recording the
/// event are one unit of work. These tests break the last step and check that the first two
/// did not survive it.
/// </summary>
public class TransactionRollbackTests(FailingPublisherApiFactory factory)
    : IClassFixture<FailingPublisherApiFactory>
{
    [Fact]
    public async Task Persists_nothing_when_recording_the_event_fails()
    {
        const string ssn = "400500600";

        var response = await factory.CreateClient().PostAsJsonAsync("/api/loan-applications", new
        {
            firstName = "Grace",
            lastName = "Hopper",
            street = "1 Compiler Court",
            city = "Arlington",
            state = "VA",
            postalCode = "22201",
            companyName = "Naval Systems",
            requestedAmount = 30_000m,
            ssn
        });

        // The failure is not swallowed; the caller is told the request did not succeed.
        Assert.False(response.IsSuccessStatusCode);

        var value = Ssn.Create(ssn);
        Assert.Equal(0, await factory.QueryDatabaseAsync(db => db.Customers.CountAsync(c => c.Ssn == value)));
        Assert.Equal(0, await factory.QueryDatabaseAsync(db => db.LoanApplications.CountAsync()));
        Assert.Equal(0, await factory.QueryDatabaseAsync(db => db.OutboxMessages.CountAsync()));
    }
}

/// <summary>
/// Proves the rollback itself rather than only its outcome.
/// <para>
/// The previous test fails before anything is written, so nothing needs undoing. Here the
/// interceptor throws after <c>SaveChanges</c> has already written all three rows inside the
/// transaction — the rows exist in the database at the moment the exception is raised, and
/// only the rollback removes them.
/// </para>
/// </summary>
public class TransactionRollbackAfterWriteTests(FailAfterWriteApiFactory factory)
    : IClassFixture<FailAfterWriteApiFactory>
{
    [Fact]
    public async Task Undoes_rows_that_were_already_written_when_the_commit_never_happens()
    {
        const string ssn = "700800900";

        var response = await factory.CreateClient().PostAsJsonAsync("/api/loan-applications", new
        {
            firstName = "Katherine",
            lastName = "Johnson",
            street = "1 Orbit Road",
            city = "Hampton",
            state = "VA",
            postalCode = "23666",
            companyName = "Flight Research",
            requestedAmount = 15_000m,
            ssn
        });

        Assert.False(response.IsSuccessStatusCode);

        // Three rows really were written inside the transaction: the customer, the
        // application and the outbox message. Without a rollback they would still be there.
        Assert.Equal(3, FailAfterWriteApiFactory.RowsWrittenBeforeFailure);

        var value = Ssn.Create(ssn);
        Assert.Equal(0, await factory.QueryDatabaseAsync(db => db.Customers.CountAsync(c => c.Ssn == value)));
        Assert.Equal(0, await factory.QueryDatabaseAsync(db => db.LoanApplications.CountAsync()));
        Assert.Equal(0, await factory.QueryDatabaseAsync(db => db.OutboxMessages.CountAsync()));
    }
}

/// <summary>
/// The same application, with the event publisher swapped for one that always throws.
/// Registering it last means it wins resolution without touching production wiring.
/// </summary>
public sealed class FailingPublisherApiFactory : LoansApiFactory
{
    protected override void ConfigureTestServices(IServiceCollection services) =>
        services.AddScoped<IEventPublisher, ThrowingEventPublisher>();

    private sealed class ThrowingEventPublisher : IEventPublisher
    {
        public void Publish(LoanApplicationRecorded @event) =>
            throw new InvalidOperationException("Simulated failure while recording the event.");
    }
}

/// <summary>
/// The same application, with an interceptor that fails once the rows have been written but
/// before the transaction is committed.
/// </summary>
public sealed class FailAfterWriteApiFactory : LoansApiFactory
{
    /// <summary>How many rows SaveChanges had written at the moment the failure was raised.</summary>
    public static int RowsWrittenBeforeFailure { get; private set; }

    protected override void ConfigureTestServices(IServiceCollection services) =>
        services.AddDbContext<LoansDbContext>(options => options
            .UseSqlite(ConnectionString)
            .AddInterceptors(new ThrowAfterSaveInterceptor()));

    private sealed class ThrowAfterSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            RowsWrittenBeforeFailure = result;
            throw new InvalidOperationException("Simulated failure after the rows were written.");
        }
    }
}
