namespace Niuro.Loans.Application.Decisions;

/// <summary>
/// Runs the deny rules over a submission. The engine knows nothing about any individual
/// rule — it only knows that a rule can object.
/// </summary>
public sealed class LoanDecisionEngine(IEnumerable<IDenyRule> rules)
{
    /// <summary>
    /// Every rule is evaluated, even once one has objected, so the applicant is told
    /// everything that is wrong instead of discovering the problems one submission at a time.
    /// </summary>
    public async Task<LoanDecision> DecideAsync(
        LoanApplicationSubmission submission,
        CancellationToken cancellationToken)
    {
        var denials = new List<Denial>();

        foreach (var rule in rules)
        {
            if (await rule.EvaluateAsync(submission, cancellationToken) is { } denial)
            {
                denials.Add(denial);
            }
        }

        return denials.Count == 0 ? LoanDecision.Approved() : LoanDecision.Denied(denials);
    }
}
