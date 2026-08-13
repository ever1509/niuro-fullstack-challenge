using Microsoft.EntityFrameworkCore;
using Niuro.Loans.Application.Persistence;
using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(LoansDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> FindBySsnAsync(Ssn ssn, CancellationToken cancellationToken) =>
        dbContext.Customers.SingleOrDefaultAsync(customer => customer.Ssn == ssn, cancellationToken);

    // Only stages the insert. The unit of work decides when — and whether — it is written.
    public void Add(Customer customer) => dbContext.Customers.Add(customer);
}
