namespace Niuro.Loans.Application.Decisions.Rules;

/// <summary>
/// Applicants whose SSN is on the blacklist are refused. The reason given back is
/// deliberately vague: telling someone they are on a list is not information we want to leak.
/// </summary>
public sealed class BlacklistedSsnRule(IBlacklistedSsnRegistry blacklist) : IDenyRule
{
    public const string DenialCode = "SSN_BLACKLISTED";

    public async Task<Denial?> EvaluateAsync(
        LoanApplicationSubmission submission,
        CancellationToken cancellationToken)
    {
        var isBlacklisted = await blacklist.ContainsAsync(submission.Ssn, cancellationToken);

        return isBlacklisted
            ? new Denial(DenialCode, "We are unable to approve this application.")
            : null;
    }
}
