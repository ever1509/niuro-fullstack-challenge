using Niuro.Loans.Application.LoanApplications;

namespace Niuro.Loans.Api.Contracts;

/// <summary>
/// The decision, in the shape the form needs to route the user.
/// </summary>
public sealed record SubmitLoanApplicationResponse(
    string Decision,
    Guid? ApplicationId,
    Guid? CustomerId,
    bool IsReturningCustomer,
    IReadOnlyList<DenialReasonResponse> Reasons)
{
    public const string Approved = "Approved";
    public const string Denied = "Denied";

    public static SubmitLoanApplicationResponse From(SubmitLoanApplicationResult result) =>
        new(
            result.IsApproved ? Approved : Denied,
            result.ApplicationId,
            result.CustomerId,
            result.IsReturningCustomer,
            [.. result.Denials.Select(d => new DenialReasonResponse(d.Code, d.Reason))]);
}

public sealed record DenialReasonResponse(string Code, string Reason);
