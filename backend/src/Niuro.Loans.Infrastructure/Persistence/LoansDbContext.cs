using Microsoft.EntityFrameworkCore;
using Niuro.Loans.Domain.Applications;
using Niuro.Loans.Domain.Customers;
using Niuro.Loans.Infrastructure.Blacklist;
using Niuro.Loans.Infrastructure.Outbox;

namespace Niuro.Loans.Infrastructure.Persistence;

public sealed class LoansDbContext(DbContextOptions<LoansDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<BlacklistedSsn> BlacklistedSsns => Set<BlacklistedSsn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LoansDbContext).Assembly);
}
