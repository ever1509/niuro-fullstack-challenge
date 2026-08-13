using Microsoft.EntityFrameworkCore;
using Niuro.Loans.Application.Decisions.Rules;
using Niuro.Loans.Infrastructure.Persistence;
using DomainSsn = Niuro.Loans.Domain.Customers.Ssn;

namespace Niuro.Loans.Infrastructure.Blacklist;

internal sealed class BlacklistedSsnRegistry(LoansDbContext dbContext) : IBlacklistedSsnRegistry
{
    public Task<bool> ContainsAsync(DomainSsn ssn, CancellationToken cancellationToken) =>
        dbContext.BlacklistedSsns.AnyAsync(entry => entry.Ssn == ssn.Value, cancellationToken);
}
