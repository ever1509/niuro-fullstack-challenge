using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Niuro.Loans.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies migrations at start-up so a fresh clone needs nothing but <c>dotnet run</c>,
    /// then switches SQLite into write-ahead logging.
    /// <para>
    /// WAL matters here because the outbox dispatcher writes from a background thread while
    /// requests are writing too. Under the default journal mode those collide and one side
    /// sees "database is locked"; under WAL a reader never blocks a writer, and the busy
    /// timeout absorbs the brief overlap between two writers.
    /// </para>
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LoansDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken);
    }
}
