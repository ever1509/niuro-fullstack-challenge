using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niuro.Loans.Application.Events;
using Niuro.Loans.Infrastructure.ExternalService;
using Niuro.Loans.Infrastructure.Persistence;

namespace Niuro.Loans.Infrastructure.Outbox;

/// <summary>
/// Delivers the events the request already committed.
/// <para>
/// This runs outside the HTTP request that answered the form, which is what makes the
/// delivery a background event rather than part of the user's wait. Because the message was
/// committed with the data, delivery can be retried until it succeeds — the request has
/// already been answered and nothing is lost if the partner is briefly unreachable.
/// </para>
/// </summary>
internal sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Outbox dispatcher started, polling every {Interval}.",
            _options.PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // A failure here must not kill the loop, or delivery stops for everyone.
                logger.LogError(exception, "Outbox poll failed; will try again next interval.");
            }

            try
            {
                await Task.Delay(_options.PollingInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Sends one batch of due messages. Separated from the loop so it can be tested
    /// directly, without waiting on a timer.
    /// </summary>
    internal async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LoansDbContext>();
        var externalService = scope.ServiceProvider.GetRequiredService<IExternalLoanService>();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var pending = await dbContext.OutboxMessages
            .Where(message =>
                message.ProcessedAtUtc == null &&
                message.Attempts < _options.MaxAttempts &&
                message.NextAttemptAtUtc <= utcNow)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            await DeliverAsync(dbContext, externalService, message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeliverAsync(
        LoansDbContext dbContext,
        IExternalLoanService externalService,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            var @event = JsonSerializer.Deserialize<LoanApplicationRecorded>(message.Payload)
                ?? throw new InvalidOperationException($"Outbox message {message.Id} has an unreadable payload.");

            var payload = await BuildPayloadAsync(dbContext, @event, cancellationToken);

            if (@event.IsNewCustomer)
            {
                await externalService.CreateAsync(payload, cancellationToken);
            }
            else
            {
                await externalService.UpdateAsync(payload, cancellationToken);
            }

            message.MarkDelivered(utcNow);

            logger.LogInformation(
                "Delivered outbox message {MessageId} for customer {CustomerId}.",
                message.Id,
                @event.CustomerId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            message.MarkFailed(exception.Message, utcNow, _options.RetryDelayAfter(message.Attempts + 1));

            logger.LogWarning(
                exception,
                "Delivery of outbox message {MessageId} failed on attempt {Attempts}.",
                message.Id,
                message.Attempts);
        }
    }

    /// <summary>
    /// Reads the records as they are now rather than as they were when the event was raised,
    /// so a delayed delivery still leaves the partner holding our current state.
    /// </summary>
    private static async Task<CustomerSyncPayload> BuildPayloadAsync(
        LoansDbContext dbContext,
        LoanApplicationRecorded @event,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(c => c.Id == @event.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {@event.CustomerId} no longer exists.");

        var application = await dbContext.LoanApplications
            .SingleOrDefaultAsync(a => a.Id == @event.ApplicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {@event.ApplicationId} no longer exists.");

        return new CustomerSyncPayload(
            customer.Id,
            application.Id,
            customer.FirstName,
            customer.LastName,
            new AddressPayload(
                customer.Address.Street,
                customer.Address.City,
                customer.Address.State,
                customer.Address.PostalCode),
            customer.CompanyName,
            customer.Ssn.Last4,
            application.RequestedAmount);
    }
}
