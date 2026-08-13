namespace Niuro.Loans.Application.Decisions;

/// <summary>
/// Why a rule refused an application. <paramref name="Code"/> is for callers to branch on,
/// <paramref name="Reason"/> is the sentence shown to the applicant.
/// </summary>
public sealed record Denial(string Code, string Reason);
