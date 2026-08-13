using Niuro.Loans.Application.Decisions;

namespace Niuro.Loans.UnitTests.Decisions;

public class LoanDecisionEngineTests
{
    [Fact]
    public async Task Approves_when_no_rule_objects()
    {
        var engine = new LoanDecisionEngine([Passes(), Passes()]);

        var decision = await engine.DecideAsync(SubmissionBuilder.Valid(), CancellationToken.None);

        Assert.True(decision.IsApproved);
        Assert.Empty(decision.Denials);
    }

    [Fact]
    public async Task Approves_when_there_are_no_rules_at_all()
    {
        var engine = new LoanDecisionEngine([]);

        var decision = await engine.DecideAsync(SubmissionBuilder.Valid(), CancellationToken.None);

        Assert.True(decision.IsApproved);
    }

    [Fact]
    public async Task Denies_when_a_single_rule_objects()
    {
        var engine = new LoanDecisionEngine([Passes(), Denies("TOO_RISKY")]);

        var decision = await engine.DecideAsync(SubmissionBuilder.Valid(), CancellationToken.None);

        Assert.False(decision.IsApproved);
        Assert.Equal("TOO_RISKY", Assert.Single(decision.Denials).Code);
    }

    [Fact]
    public async Task Reports_every_reason_rather_than_stopping_at_the_first()
    {
        var engine = new LoanDecisionEngine([Denies("FIRST"), Passes(), Denies("SECOND")]);

        var decision = await engine.DecideAsync(SubmissionBuilder.Valid(), CancellationToken.None);

        Assert.Equal(["FIRST", "SECOND"], decision.Denials.Select(d => d.Code));
    }

    [Fact]
    public async Task Picks_up_a_new_rule_without_any_change_to_the_engine()
    {
        // The point of the design: a rule the engine has never heard of still takes effect.
        var engine = new LoanDecisionEngine([new ApplicantIsNamedAdaRule()]);

        var decision = await engine.DecideAsync(SubmissionBuilder.Valid(), CancellationToken.None);

        Assert.False(decision.IsApproved);
        Assert.Equal("NAMED_ADA", Assert.Single(decision.Denials).Code);
    }

    private static IDenyRule Passes() => new StubRule(null);

    private static IDenyRule Denies(string code) => new StubRule(new Denial(code, code));

    private sealed class StubRule(Denial? denial) : IDenyRule
    {
        public Task<Denial?> EvaluateAsync(LoanApplicationSubmission submission, CancellationToken cancellationToken) =>
            Task.FromResult(denial);
    }

    private sealed class ApplicantIsNamedAdaRule : IDenyRule
    {
        public Task<Denial?> EvaluateAsync(LoanApplicationSubmission submission, CancellationToken cancellationToken) =>
            Task.FromResult<Denial?>(
                submission.FirstName == "Ada" ? new Denial("NAMED_ADA", "No Adas.") : null);
    }
}
