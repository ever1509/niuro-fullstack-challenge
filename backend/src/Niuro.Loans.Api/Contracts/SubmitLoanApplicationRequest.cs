using System.ComponentModel.DataAnnotations;

namespace Niuro.Loans.Api.Contracts;

/// <summary>
/// The form as JSON. The annotations catch missing or malformed fields at the edge and
/// answer with per-field errors the form can display; the domain still guards its own
/// invariants for anything that gets past here.
/// </summary>
public sealed record SubmitLoanApplicationRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string FirstName { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public required string LastName { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string Street { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public required string City { get; init; }

    [Required, StringLength(2, MinimumLength = 2)]
    public required string State { get; init; }

    [Required, StringLength(10, MinimumLength = 5)]
    public required string PostalCode { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string CompanyName { get; init; }

    [Range(0.01, 1_000_000)]
    public required decimal RequestedAmount { get; init; }

    /// <summary>Accepted with or without dashes; the domain normalises it.</summary>
    [Required, RegularExpression(@"^\d{3}-?\d{2}-?\d{4}$", ErrorMessage = "SSN must be 9 digits.")]
    public required string Ssn { get; init; }
}
