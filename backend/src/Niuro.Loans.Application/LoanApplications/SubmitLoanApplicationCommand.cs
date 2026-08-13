namespace Niuro.Loans.Application.LoanApplications;

/// <summary>
/// The form as it arrives from the outside world: unvalidated strings. Turning these into
/// domain types is the use case's first job, and the point where malformed input is rejected.
/// </summary>
public sealed record SubmitLoanApplicationCommand(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string PostalCode,
    string CompanyName,
    decimal RequestedAmount,
    string Ssn);
