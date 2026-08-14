using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Niuro.Loans.Infrastructure.Persistence;

namespace Niuro.Loans.IntegrationTests;

/// <summary>
/// Boots the real application against a throwaway SQLite file.
/// <para>
/// A file rather than an in-memory provider on purpose: the behaviour under test is a
/// transaction rolling back, and only a real database can demonstrate that.
/// </para>
/// </summary>
public class LoansApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"niuro-loans-tests-{Guid.NewGuid():N}.db");

    /// <summary>Exposed so a derived factory can re-register the context with interceptors.</summary>
    protected string ConnectionString => $"Data Source={_databasePath}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:LoansDatabase", ConnectionString);

        builder.ConfigureServices(ConfigureTestServices);
    }

    /// <summary>Hook for tests that need to replace a dependency, such as forcing a failure.</summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    public async Task<T> QueryDatabaseAsync<T>(Func<LoansDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LoansDbContext>();
        return await query(dbContext);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();

        foreach (var file in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
