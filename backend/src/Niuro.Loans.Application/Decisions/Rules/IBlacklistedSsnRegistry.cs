using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.Application.Decisions.Rules;

/// <summary>
/// The set of SSNs we refuse to lend to. Declared here and implemented in Infrastructure
/// so the rule stays testable without a database.
/// </summary>
public interface IBlacklistedSsnRegistry
{
    Task<bool> ContainsAsync(Ssn ssn, CancellationToken cancellationToken);
}
