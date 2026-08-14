using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Niuro.Loans.Infrastructure.ExternalService;
using Niuro.Loans.Infrastructure.Outbox;
using Niuro.Loans.Infrastructure.Persistence;

namespace Niuro.Loans.IntegrationTests;

/// <summary>
/// Drives the dispatcher one batch at a time against a real database, with the partner
/// system faked. What is under test is the delivery decision and the retry behaviour,
/// not HTTP itself.
/// </summary>
public sealed class OutboxDispatcherTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"niuro-outbox-tests-{Guid.NewGuid():N}.db");

    private ServiceProvider _services = null!;
    private FakeExternalLoanService _externalService = null!;

    public async Task InitializeAsync()
    {
        _externalService = new FakeExternalLoanService();

        var services = new ServiceCollection();
        services.AddDbContext<LoansDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
        services.AddSingleton<IExternalLoanService>(_externalService);
        _services = services.BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LoansDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();

        foreach (var file in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task Creates_in_the_external_service_for_a_first_time_customer()
    {
        var (customerId, _) = await SeedApprovedApplicationAsync(isNewCustomer: true);

        await DispatchAsync();

        var sent = Assert.Single(_externalService.Created);
        Assert.Equal(customerId, sent.CustomerId);
        Assert.Empty(_externalService.Updated);
        Assert.Equal(0, await CountPendingAsync());
    }

    [Fact]
    public async Task Updates_in_the_external_service_for_a_returning_customer()
    {
        await SeedApprovedApplicationAsync(isNewCustomer: false);

        await DispatchAsync();

        Assert.Single(_externalService.Updated);
        Assert.Empty(_externalService.Created);
    }

    [Fact]
    public async Task Sends_only_the_last_four_digits_of_the_ssn()
    {
        await SeedApprovedApplicationAsync(isNewCustomer: true, ssn: "123456789");

        await DispatchAsync();

        var sent = Assert.Single(_externalService.Created);
        Assert.Equal("6789", sent.SsnLast4);

        // The full number must not leave the building in any field.
        var serialised = System.Text.Json.JsonSerializer.Serialize(sent);
        Assert.DoesNotContain("123456789", serialised);
    }

    [Fact]
    public async Task Leaves_the_message_pending_when_the_external_service_is_down()
    {
        await SeedApprovedApplicationAsync(isNewCustomer: true);
        _externalService.FailNext(1);

        await DispatchAsync();

        Assert.Equal(1, await CountPendingAsync());
        var message = await SingleMessageAsync();
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.ProcessedAtUtc);
        Assert.NotNull(message.LastError);
    }

    [Fact]
    public async Task Delivers_on_a_later_attempt_once_the_external_service_recovers()
    {
        await SeedApprovedApplicationAsync(isNewCustomer: true);
        _externalService.FailNext(1);

        await DispatchAsync();
        // The failed message backs off, so the retry has to happen after that delay.
        await DispatchAsync(advance: TimeSpan.FromMinutes(1));

        Assert.Single(_externalService.Created);
        Assert.Equal(0, await CountPendingAsync());
    }

    [Fact]
    public async Task Does_not_retry_before_the_backoff_has_elapsed()
    {
        await SeedApprovedApplicationAsync(isNewCustomer: true);
        _externalService.FailNext(2);

        await DispatchAsync();
        await DispatchAsync();

        // The second poll happened immediately, so the message was not due yet.
        Assert.Equal(1, (await SingleMessageAsync()).Attempts);
    }

    [Fact]
    public async Task Gives_up_after_the_attempt_limit_and_leaves_the_message_for_inspection()
    {
        await SeedApprovedApplicationAsync(isNewCustomer: true);
        _externalService.FailNext(100);

        var options = OptionsFor();
        for (var attempt = 0; attempt < options.MaxAttempts + 3; attempt++)
        {
            await DispatchAsync(advance: TimeSpan.FromMinutes(10) * attempt);
        }

        var message = await SingleMessageAsync();
        Assert.Equal(options.MaxAttempts, message.Attempts);
        Assert.Null(message.ProcessedAtUtc);
    }

    [Fact]
    public async Task Does_nothing_when_there_is_nothing_to_send()
    {
        await DispatchAsync();

        Assert.Empty(_externalService.Created);
        Assert.Empty(_externalService.Updated);
    }

    private static OutboxOptions OptionsFor() => new()
    {
        PollingInterval = TimeSpan.FromMilliseconds(10),
        MaxAttempts = 3,
        InitialRetryDelay = TimeSpan.FromSeconds(30)
    };

    private async Task DispatchAsync(TimeSpan advance = default)
    {
        var timeProvider = new AdvancingTimeProvider(DateTimeOffset.UtcNow + advance);

        var dispatcher = new OutboxDispatcher(
            _services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(OptionsFor()),
            timeProvider,
            NullLogger<OutboxDispatcher>.Instance);

        await dispatcher.DispatchPendingAsync(CancellationToken.None);
    }

    private async Task<(Guid CustomerId, Guid ApplicationId)> SeedApprovedApplicationAsync(
        bool isNewCustomer,
        string ssn = "555667777")
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LoansDbContext>();
        var utcNow = DateTime.UtcNow;

        var customer = Domain.Customers.Customer.Register(
            Domain.Customers.Ssn.Create(ssn),
            "Ada",
            "Lovelace",
            Domain.Customers.Address.Create("1 Analytical Way", "San Francisco", "CA", "94105"),
            "Analytical Engines Inc.",
            utcNow);

        var application = Domain.Applications.LoanApplication.Submit(customer.Id, 25_000m, utcNow);

        dbContext.Customers.Add(customer);
        dbContext.LoanApplications.Add(application);
        dbContext.OutboxMessages.Add(OutboxMessage.For(
            nameof(Application.Events.LoanApplicationRecorded),
            System.Text.Json.JsonSerializer.Serialize(
                new Application.Events.LoanApplicationRecorded(customer.Id, application.Id, isNewCustomer)),
            utcNow));

        await dbContext.SaveChangesAsync();

        return (customer.Id, application.Id);
    }

    private async Task<int> CountPendingAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LoansDbContext>()
            .OutboxMessages.CountAsync(m => m.ProcessedAtUtc == null);
    }

    private async Task<OutboxMessage> SingleMessageAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LoansDbContext>()
            .OutboxMessages.AsNoTracking().SingleAsync();
    }

    /// <summary>A clock fixed at a chosen moment, so backoff can be tested without waiting.</summary>
    private sealed class AdvancingTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeExternalLoanService : IExternalLoanService
    {
        private int _failuresRemaining;

        public List<CustomerSyncPayload> Created { get; } = [];
        public List<CustomerSyncPayload> Updated { get; } = [];

        public void FailNext(int count) => _failuresRemaining = count;

        public Task CreateAsync(CustomerSyncPayload payload, CancellationToken cancellationToken)
        {
            ThrowIfFailing();
            Created.Add(payload);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CustomerSyncPayload payload, CancellationToken cancellationToken)
        {
            ThrowIfFailing();
            Updated.Add(payload);
            return Task.CompletedTask;
        }

        private void ThrowIfFailing()
        {
            if (_failuresRemaining > 0)
            {
                _failuresRemaining--;
                throw new HttpRequestException("Simulated outage.");
            }
        }
    }
}
