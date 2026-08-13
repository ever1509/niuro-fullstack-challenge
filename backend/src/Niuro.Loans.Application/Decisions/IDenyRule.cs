namespace Niuro.Loans.Application.Decisions;

/// <summary>
/// One reason an application might be refused.
/// <para>
/// Implementations are discovered through dependency injection, so adding a rule means
/// adding a class and registering it — no existing rule and no existing test changes.
/// </para>
/// </summary>
public interface IDenyRule
{
    /// <summary>
    /// Returns a <see cref="Denial"/> when this rule refuses the submission, or
    /// <see langword="null"/> when it has no objection.
    /// </summary>
    Task<Denial?> EvaluateAsync(LoanApplicationSubmission submission, CancellationToken cancellationToken);
}
