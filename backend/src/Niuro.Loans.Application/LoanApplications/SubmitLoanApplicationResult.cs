using Niuro.Loans.Application.Decisions;

namespace Niuro.Loans.Application.LoanApplications;

/// <summary>
/// What happened to a submission. A denial is a normal outcome, not a failure — the API
/// answers both cases with 200 and lets the caller read the decision.
/// </summary>
public sealed record SubmitLoanApplicationResult
{
    private SubmitLoanApplicationResult(
        Guid? customerId,
        Guid? applicationId,
        bool isReturningCustomer,
        IReadOnlyList<Denial> denials)
    {
        CustomerId = customerId;
        ApplicationId = applicationId;
        IsReturningCustomer = isReturningCustomer;
        Denials = denials;
    }

    public Guid? CustomerId { get; }
    public Guid? ApplicationId { get; }

    /// <summary>True when this submission updated records that already existed.</summary>
    public bool IsReturningCustomer { get; }

    public IReadOnlyList<Denial> Denials { get; }

    public bool IsApproved => Denials.Count == 0;

    public static SubmitLoanApplicationResult Approved(
        Guid customerId,
        Guid applicationId,
        bool isReturningCustomer) =>
        new(customerId, applicationId, isReturningCustomer, []);

    public static SubmitLoanApplicationResult Denied(IReadOnlyList<Denial> denials) =>
        denials.Count > 0
            ? new SubmitLoanApplicationResult(null, null, false, denials)
            : throw new ArgumentException("A denial needs at least one reason.", nameof(denials));
}
