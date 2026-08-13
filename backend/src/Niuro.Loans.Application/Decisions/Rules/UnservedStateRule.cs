using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.Application.Decisions.Rules;

/// <summary>
/// We are not licensed to lend in every state. Applicants living in one we do not serve
/// are refused regardless of anything else on the form.
/// </summary>
public sealed class UnservedStateRule : IDenyRule
{
    public const string DenialCode = "STATE_NOT_SERVED";

    private static readonly HashSet<string> UnservedStates = ["NY"];

    public Task<Denial?> EvaluateAsync(
        LoanApplicationSubmission submission,
        CancellationToken cancellationToken)
    {
        var state = submission.Address.State;

        var denial = UnservedStates.Contains(state)
            ? new Denial(DenialCode, $"We do not currently issue loans in {state}.")
            : null;

        return Task.FromResult(denial);
    }
}
