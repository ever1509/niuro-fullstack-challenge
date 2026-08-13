using Niuro.Loans.Application.Decisions.Rules;
using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.UnitTests.Decisions;

public class BlacklistedSsnRuleTests
{
    private const string BlacklistedSsn = "666554444";

    [Fact]
    public async Task Denies_an_applicant_whose_ssn_is_on_the_blacklist()
    {
        var rule = new BlacklistedSsnRule(new FakeBlacklist(BlacklistedSsn));

        var denial = await rule.EvaluateAsync(
            SubmissionBuilder.Valid(ssn: BlacklistedSsn),
            CancellationToken.None);

        Assert.NotNull(denial);
        Assert.Equal(BlacklistedSsnRule.DenialCode, denial.Code);
    }

    [Fact]
    public async Task Matches_the_blacklist_even_when_the_ssn_is_typed_with_dashes()
    {
        var rule = new BlacklistedSsnRule(new FakeBlacklist(BlacklistedSsn));

        var denial = await rule.EvaluateAsync(
            SubmissionBuilder.Valid(ssn: "666-55-4444"),
            CancellationToken.None);

        Assert.NotNull(denial);
    }

    [Fact]
    public async Task Has_no_objection_to_an_ssn_that_is_not_listed()
    {
        var rule = new BlacklistedSsnRule(new FakeBlacklist(BlacklistedSsn));

        var denial = await rule.EvaluateAsync(SubmissionBuilder.Valid(), CancellationToken.None);

        Assert.Null(denial);
    }

    [Fact]
    public async Task Does_not_reveal_that_the_applicant_is_on_a_list()
    {
        var rule = new BlacklistedSsnRule(new FakeBlacklist(BlacklistedSsn));

        var denial = await rule.EvaluateAsync(
            SubmissionBuilder.Valid(ssn: BlacklistedSsn),
            CancellationToken.None);

        Assert.NotNull(denial);
        Assert.DoesNotContain("blacklist", denial.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(BlacklistedSsn, denial.Reason);
    }

    private sealed class FakeBlacklist(params string[] ssns) : IBlacklistedSsnRegistry
    {
        public Task<bool> ContainsAsync(Ssn ssn, CancellationToken cancellationToken) =>
            Task.FromResult(ssns.Contains(ssn.Value));
    }
}
