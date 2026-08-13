namespace Niuro.Loans.Application.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction and commits it.
    /// Everything staged during the operation — the customer, the application and the
    /// event waiting to be sent — is written together or not at all. If the operation
    /// throws, nothing is persisted.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
