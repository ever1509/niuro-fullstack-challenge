using Niuro.Loans.Application.Events;
using Niuro.Loans.Application.Persistence;
using Niuro.Loans.Domain.Applications;
using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.UnitTests.LoanApplications;

/// <summary>
/// Hand-written stand-ins for the ports. They are small enough that a mocking library
/// would add a dependency without making the tests any clearer.
/// </summary>
internal sealed class InMemoryCustomerRepository : ICustomerRepository
{
    public List<Customer> Customers { get; } = [];

    public Task<Customer?> FindBySsnAsync(Ssn ssn, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.SingleOrDefault(c => c.Ssn == ssn));

    public void Add(Customer customer) => Customers.Add(customer);
}

internal sealed class InMemoryLoanApplicationRepository : ILoanApplicationRepository
{
    public List<LoanApplication> Applications { get; } = [];

    public Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(Applications.SingleOrDefault(a => a.CustomerId == customerId));

    public void Add(LoanApplication application) => Applications.Add(application);
}

internal sealed class RecordingEventPublisher : IEventPublisher
{
    public List<LoanApplicationRecorded> Published { get; } = [];

    public void Publish(LoanApplicationRecorded @event) => Published.Add(@event);
}

/// <summary>
/// Runs the operation without a real transaction. The rollback guarantee itself is proved
/// against a real database in the integration tests, not here.
/// </summary>
internal sealed class PassThroughUnitOfWork : IUnitOfWork
{
    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken) => operation(cancellationToken);
}
