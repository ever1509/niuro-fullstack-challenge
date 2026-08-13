using Niuro.Loans.Application.Persistence;

namespace Niuro.Loans.Infrastructure.Persistence;

internal sealed class UnitOfWork(LoansDbContext dbContext) : IUnitOfWork
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);

            // One SaveChanges writes the customer, the application and the outbox row together.
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            // Disposing an uncommitted transaction rolls it back; the explicit call makes
            // the intent obvious to anyone reading this.
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
