using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Loans.Domain.Applications;

namespace Niuro.Loans.Infrastructure.Persistence.Configurations;

internal sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.ToTable("LoanApplications");
        builder.HasKey(a => a.Id);

        // No navigation property to Customer: they are separate aggregates and this side
        // holds only the identity. The unique index is the "one application per customer"
        // rule, expressed where it cannot be bypassed.
        builder.HasIndex(a => a.CustomerId).IsUnique();

        builder.Property(a => a.RequestedAmount)
            .HasPrecision(18, 2)
            .IsRequired();
    }
}
