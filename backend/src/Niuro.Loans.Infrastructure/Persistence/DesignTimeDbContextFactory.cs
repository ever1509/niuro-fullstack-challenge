using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Niuro.Loans.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> when generating migrations, so the tooling does not have to
/// boot the API to discover the model. The connection string here is never used at runtime.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LoansDbContext>
{
    public LoansDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LoansDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new LoansDbContext(options);
    }
}
