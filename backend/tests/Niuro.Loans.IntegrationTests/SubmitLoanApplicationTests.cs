using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.IntegrationTests;

public class SubmitLoanApplicationTests(LoansApiFactory factory) : IClassFixture<LoansApiFactory>
{
    private const string SeededBlacklistedSsn = "666-55-4444";

    [Fact]
    public async Task Approves_a_clean_application_and_records_everything_in_one_go()
    {
        const string ssn = "100200300";

        var response = await Submit(Form(ssn, requestedAmount: 25_000m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Read(response);
        Assert.Equal("Approved", body.Decision);
        Assert.False(body.IsReturningCustomer);
        Assert.NotNull(body.CustomerId);
        Assert.NotNull(body.ApplicationId);

        // Customer, application and the pending event were all written by the one transaction.
        Assert.Equal(1, await CountCustomersAsync(ssn));
        Assert.Equal(1, await CountApplicationsAsync(body.CustomerId!.Value));
        Assert.Equal(1, await CountOutboxAsync(body.CustomerId!.Value));
    }

    [Fact]
    public async Task Denies_an_applicant_from_New_York_without_writing_anything()
    {
        const string ssn = "100200301";
        var outboxBefore = await CountAllOutboxAsync();

        var response = await Submit(Form(ssn, state: "NY"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Read(response);
        Assert.Equal("Denied", body.Decision);
        Assert.Equal("STATE_NOT_SERVED", Assert.Single(body.Reasons).Code);
        Assert.Null(body.ApplicationId);

        Assert.Equal(0, await CountCustomersAsync(ssn));
        Assert.Equal(outboxBefore, await CountAllOutboxAsync());
    }

    [Fact]
    public async Task Denies_a_blacklisted_ssn_without_writing_anything()
    {
        var outboxBefore = await CountAllOutboxAsync();

        var response = await Submit(Form(SeededBlacklistedSsn));

        var body = await Read(response);
        Assert.Equal("Denied", body.Decision);
        Assert.Equal("SSN_BLACKLISTED", Assert.Single(body.Reasons).Code);

        Assert.Equal(0, await CountCustomersAsync(SeededBlacklistedSsn));
        Assert.Equal(outboxBefore, await CountAllOutboxAsync());
    }

    [Fact]
    public async Task Reports_every_reason_when_more_than_one_rule_objects()
    {
        var body = await Read(await Submit(Form(SeededBlacklistedSsn, state: "NY")));

        Assert.Equal("Denied", body.Decision);
        Assert.Equal(2, body.Reasons.Count);
    }

    [Fact]
    public async Task Updates_the_existing_records_when_the_same_ssn_applies_again()
    {
        const string ssn = "222334444";

        var first = await Read(await Submit(Form(ssn, requestedAmount: 10_000m, companyName: "First Job Inc.")));
        var second = await Read(await Submit(Form(ssn, requestedAmount: 45_000m, companyName: "Second Job Inc.")));

        Assert.Equal("Approved", second.Decision);
        Assert.True(second.IsReturningCustomer);

        // The same records were amended, not replaced.
        Assert.Equal(first.CustomerId, second.CustomerId);
        Assert.Equal(first.ApplicationId, second.ApplicationId);

        Assert.Equal(1, await CountCustomersAsync(ssn));
        Assert.Equal(1, await CountApplicationsAsync(second.CustomerId!.Value));

        // Two events: the create, then the update.
        Assert.Equal(2, await CountOutboxAsync(second.CustomerId!.Value));

        var application = await factory.QueryDatabaseAsync(db =>
            db.LoanApplications.SingleAsync(a => a.CustomerId == second.CustomerId!.Value));
        Assert.Equal(45_000m, application.RequestedAmount);

        var customer = await factory.QueryDatabaseAsync(db =>
            db.Customers.SingleAsync(c => c.Id == second.CustomerId!.Value));
        Assert.Equal("Second Job Inc.", customer.CompanyName);
    }

    [Fact]
    public async Task Recognises_a_returning_customer_who_typed_the_ssn_with_dashes()
    {
        await Submit(Form("313131313"));

        var second = await Read(await Submit(Form("313-13-1313")));

        Assert.True(second.IsReturningCustomer);
        Assert.Equal(1, await CountCustomersAsync("313131313"));
    }

    [Fact]
    public async Task Rejects_a_malformed_ssn_with_a_bad_request()
    {
        var response = await Submit(Form(ssn: "123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_negative_amount_with_a_bad_request()
    {
        var response = await Submit(Form("100200302", requestedAmount: -1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> Submit(object form) =>
        factory.CreateClient().PostAsJsonAsync("/api/loan-applications", form);

    private static async Task<SubmitResponse> Read(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<SubmitResponse>())!;

    private Task<int> CountCustomersAsync(string ssn)
    {
        var value = Ssn.Create(ssn);
        return factory.QueryDatabaseAsync(db => db.Customers.CountAsync(c => c.Ssn == value));
    }

    private Task<int> CountApplicationsAsync(Guid customerId) =>
        factory.QueryDatabaseAsync(db => db.LoanApplications.CountAsync(a => a.CustomerId == customerId));

    /// <summary>Events are correlated by the customer id embedded in the serialised payload.</summary>
    private Task<int> CountOutboxAsync(Guid customerId)
    {
        var id = customerId.ToString();
        return factory.QueryDatabaseAsync(db => db.OutboxMessages.CountAsync(m => m.Payload.Contains(id)));
    }

    private Task<int> CountAllOutboxAsync() =>
        factory.QueryDatabaseAsync(db => db.OutboxMessages.CountAsync());

    private static object Form(
        string ssn,
        string state = "CA",
        decimal requestedAmount = 10_000m,
        string companyName = "Analytical Engines Inc.") =>
        new
        {
            firstName = "Ada",
            lastName = "Lovelace",
            street = "1 Analytical Way",
            city = "San Francisco",
            state,
            postalCode = "94105",
            companyName,
            requestedAmount,
            ssn
        };

    private sealed record SubmitResponse(
        string Decision,
        Guid? ApplicationId,
        Guid? CustomerId,
        bool IsReturningCustomer,
        IReadOnlyList<DenialReason> Reasons);

    private sealed record DenialReason(string Code, string Reason);
}
