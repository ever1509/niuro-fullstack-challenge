using Niuro.Loans.Application.Decisions.Rules;

namespace Niuro.Loans.UnitTests.Decisions;

public class UnservedStateRuleTests
{
    private readonly UnservedStateRule _rule = new();

    [Fact]
    public async Task Denies_an_applicant_living_in_New_York()
    {
        var denial = await _rule.EvaluateAsync(SubmissionBuilder.Valid(state: "NY"), CancellationToken.None);

        Assert.NotNull(denial);
        Assert.Equal(UnservedStateRule.DenialCode, denial.Code);
    }

    [Fact]
    public async Task Denies_New_York_regardless_of_how_the_state_was_typed()
    {
        // Address normalises the state on construction, so "ny" and "NY" are the same rule input.
        var denial = await _rule.EvaluateAsync(SubmissionBuilder.Valid(state: "ny"), CancellationToken.None);

        Assert.NotNull(denial);
    }

    [Theory]
    [InlineData("CA")]
    [InlineData("TX")]
    [InlineData("FL")]
    public async Task Has_no_objection_to_a_state_we_serve(string state)
    {
        var denial = await _rule.EvaluateAsync(SubmissionBuilder.Valid(state: state), CancellationToken.None);

        Assert.Null(denial);
    }
}
