using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.Application.Persistence;

public interface ICustomerRepository
{
    /// <summary>
    /// Looks a customer up by the identity the business uses. Returning
    /// <see langword="null"/> is what tells the use case this is a first application.
    /// </summary>
    Task<Customer?> FindBySsnAsync(Ssn ssn, CancellationToken cancellationToken);

    void Add(Customer customer);
}
