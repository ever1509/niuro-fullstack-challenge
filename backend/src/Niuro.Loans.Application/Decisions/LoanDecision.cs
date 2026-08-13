namespace Niuro.Loans.Application.Decisions;

/// <summary>
/// The outcome of running every rule against a submission.
/// </summary>
public sealed record LoanDecision
{
    private LoanDecision(IReadOnlyList<Denial> denials) => Denials = denials;

    public IReadOnlyList<Denial> Denials { get; }

    public bool IsApproved => Denials.Count == 0;

    public static LoanDecision Approved() => new([]);

    public static LoanDecision Denied(IReadOnlyList<Denial> denials) =>
        denials.Count > 0
            ? new LoanDecision(denials)
            : throw new ArgumentException("A denial needs at least one reason.", nameof(denials));
}
