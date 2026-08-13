using Microsoft.EntityFrameworkCore;
using Niuro.Loans.Application.Persistence;
using Niuro.Loans.Domain.Applications;

namespace Niuro.Loans.Infrastructure.Persistence.Repositories;

internal sealed class LoanApplicationRepository(LoansDbContext dbContext) : ILoanApplicationRepository
{
    public Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        dbContext.LoanApplications
            .SingleOrDefaultAsync(application => application.CustomerId == customerId, cancellationToken);

    public void Add(LoanApplication application) => dbContext.LoanApplications.Add(application);
}
