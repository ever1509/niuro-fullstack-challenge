using Niuro.Loans.Application.Decisions;
using Niuro.Loans.Application.Decisions.Rules;
using Niuro.Loans.Application.LoanApplications;
using Niuro.Loans.Domain;

namespace Niuro.Loans.UnitTests.LoanApplications;

public class SubmitLoanApplicationHandlerTests
{
    private const string Ssn = "111223333";
    private const string BlacklistedSsn = "666554444";

    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryLoanApplicationRepository _applications = new();
    private readonly RecordingEventPublisher _events = new();

    [Fact]
    public async Task Creates_a_customer_and_an_application_for_a_first_time_applicant()
    {
        var result = await Submit(CommandFor(Ssn, requestedAmount: 25_000m));

        Assert.True(result.IsApproved);
        Assert.False(result.IsReturningCustomer);

        var customer = Assert.Single(_customers.Customers);
        var application = Assert.Single(_applications.Applications);
        Assert.Equal(customer.Id, application.CustomerId);
        Assert.Equal(25_000m, application.RequestedAmount);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(application.Id, result.ApplicationId);
    }

    [Fact]
    public async Task Publishes_a_create_event_for_a_first_time_applicant()
    {
        await Submit(CommandFor(Ssn));

        var published = Assert.Single(_events.Published);
        Assert.True(published.IsNewCustomer);
        Assert.Equal(Assert.Single(_customers.Customers).Id, published.CustomerId);
    }

    [Fact]
    public async Task Updates_the_existing_records_when_the_same_ssn_applies_again()
    {
        await Submit(CommandFor(Ssn, requestedAmount: 10_000m, companyName: "First Job Inc."));

        var result = await Submit(CommandFor(Ssn, requestedAmount: 40_000m, companyName: "Second Job Inc."));

        Assert.True(result.IsReturningCustomer);

        // The requirement: same SSN means one customer and one application, updated.
        var customer = Assert.Single(_customers.Customers);
        var application = Assert.Single(_applications.Applications);
        Assert.Equal("Second Job Inc.", customer.CompanyName);
        Assert.Equal(40_000m, application.RequestedAmount);
    }

    [Fact]
    public async Task Keeps_the_same_identifiers_when_a_customer_applies_again()
    {
        var first = await Submit(CommandFor(Ssn));

        var second = await Submit(CommandFor(Ssn, requestedAmount: 40_000m));

        Assert.Equal(first.CustomerId, second.CustomerId);
        Assert.Equal(first.ApplicationId, second.ApplicationId);
    }

    [Fact]
    public async Task Recognises_a_returning_customer_who_typed_the_ssn_differently()
    {
        await Submit(CommandFor("111223333"));

        var result = await Submit(CommandFor("111-22-3333"));

        Assert.True(result.IsReturningCustomer);
        Assert.Single(_customers.Customers);
    }

    [Fact]
    public async Task Publishes_an_update_event_for_a_returning_customer()
    {
        await Submit(CommandFor(Ssn));

        await Submit(CommandFor(Ssn, requestedAmount: 40_000m));

        Assert.Equal(2, _events.Published.Count);
        Assert.True(_events.Published[0].IsNewCustomer);
        Assert.False(_events.Published[1].IsNewCustomer);
    }

    [Fact]
    public async Task Writes_nothing_and_publishes_nothing_when_the_application_is_denied()
    {
        var result = await Submit(CommandFor(Ssn, state: "NY"));

        Assert.False(result.IsApproved);
        Assert.Equal(UnservedStateRule.DenialCode, Assert.Single(result.Denials).Code);
        Assert.Empty(_customers.Customers);
        Assert.Empty(_applications.Applications);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Reports_every_reason_when_more_than_one_rule_objects()
    {
        var result = await Submit(CommandFor(BlacklistedSsn, state: "NY"));

        Assert.False(result.IsApproved);
        Assert.Equal(2, result.Denials.Count);
    }

    [Fact]
    public async Task Rejects_a_malformed_submission_before_any_rule_runs()
    {
        await Assert.ThrowsAsync<DomainException>(() => Submit(CommandFor(ssn: "12345")));

        Assert.Empty(_customers.Customers);
    }

    private Task<SubmitLoanApplicationResult> Submit(SubmitLoanApplicationCommand command)
    {
        var engine = new LoanDecisionEngine(
        [
            new UnservedStateRule(),
            new BlacklistedSsnRule(new StaticBlacklist(BlacklistedSsn))
        ]);

        var handler = new SubmitLoanApplicationHandler(
            engine,
            _customers,
            _applications,
            _events,
            new PassThroughUnitOfWork(),
            TimeProvider.System);

        return handler.HandleAsync(command, CancellationToken.None);
    }

    private static SubmitLoanApplicationCommand CommandFor(
        string ssn,
        string state = "CA",
        decimal requestedAmount = 10_000m,
        string companyName = "Analytical Engines Inc.") =>
        new(
            FirstName: "Ada",
            LastName: "Lovelace",
            Street: "1 Analytical Way",
            City: "San Francisco",
            State: state,
            PostalCode: "94105",
            CompanyName: companyName,
            RequestedAmount: requestedAmount,
            Ssn: ssn);

    private sealed class StaticBlacklist(params string[] ssns) : IBlacklistedSsnRegistry
    {
        public Task<bool> ContainsAsync(Domain.Customers.Ssn ssn, CancellationToken cancellationToken) =>
            Task.FromResult(ssns.Contains(ssn.Value));
    }
}
