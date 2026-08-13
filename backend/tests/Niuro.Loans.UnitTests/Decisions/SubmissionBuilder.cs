using Niuro.Loans.Application.Decisions;
using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.UnitTests.Decisions;

/// <summary>
/// Builds a submission that passes every rule, so each test only states the one field
/// it cares about and the reader can see immediately what is under test.
/// </summary>
internal static class SubmissionBuilder
{
    public const string CleanSsn = "111223333";

    public static LoanApplicationSubmission Valid(
        string ssn = CleanSsn,
        string state = "CA",
        decimal requestedAmount = 10_000m) =>
        new(
            Ssn.Create(ssn),
            FirstName: "Ada",
            LastName: "Lovelace",
            Address: Address.Create("1 Analytical Way", "San Francisco", state, "94105"),
            CompanyName: "Analytical Engines Inc.",
            RequestedAmount: requestedAmount);
}
