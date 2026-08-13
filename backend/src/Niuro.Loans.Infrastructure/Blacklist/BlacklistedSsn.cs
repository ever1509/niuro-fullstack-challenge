namespace Niuro.Loans.Infrastructure.Blacklist;

/// <summary>
/// An SSN we refuse to lend to. Stored as data rather than code because the list grows
/// and shrinks without a deployment.
/// </summary>
public sealed class BlacklistedSsn
{
    private BlacklistedSsn()
    {
        Ssn = null!;
    }

    public BlacklistedSsn(string ssn, string? reason = null)
    {
        Ssn = ssn;
        Reason = reason;
    }

    /// <summary>The nine digits, normalised the same way <c>Domain.Customers.Ssn</c> normalises them.</summary>
    public string Ssn { get; private init; }

    public string? Reason { get; private init; }
}
