namespace Niuro.Loans.Infrastructure.ExternalService;

/// <summary>
/// What the external service is told about an approved application.
/// <para>
/// Only the last four digits of the SSN are sent. The partner keys its records on our
/// customer id, so it never needs the full number, and the cheapest way to protect a secret
/// is not to transmit it.
/// </para>
/// </summary>
public sealed record CustomerSyncPayload(
    Guid CustomerId,
    Guid ApplicationId,
    string FirstName,
    string LastName,
    AddressPayload Address,
    string CompanyName,
    string SsnLast4,
    decimal RequestedAmount);

public sealed record AddressPayload(string Street, string City, string State, string PostalCode);
