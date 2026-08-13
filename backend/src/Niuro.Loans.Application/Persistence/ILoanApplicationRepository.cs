using Niuro.Loans.Domain.Applications;

namespace Niuro.Loans.Application.Persistence;

public interface ILoanApplicationRepository
{
    /// <summary>
    /// A customer has at most one application: a repeat submission amends it rather
    /// than opening another.
    /// </summary>
    Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(LoanApplication application);
}
