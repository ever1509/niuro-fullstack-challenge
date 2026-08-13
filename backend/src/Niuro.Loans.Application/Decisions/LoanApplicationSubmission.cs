using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.Application.Decisions;

/// <summary>
/// One filled-in form, already parsed into domain types. Anything that reaches this point
/// is structurally valid, so the rules only decide eligibility, never shape.
/// </summary>
public sealed record LoanApplicationSubmission(
    Ssn Ssn,
    string FirstName,
    string LastName,
    Address Address,
    string CompanyName,
    decimal RequestedAmount);
