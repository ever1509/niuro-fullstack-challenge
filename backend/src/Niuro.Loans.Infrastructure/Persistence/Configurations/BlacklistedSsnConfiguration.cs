using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Loans.Infrastructure.Blacklist;

namespace Niuro.Loans.Infrastructure.Persistence.Configurations;

internal sealed class BlacklistedSsnConfiguration : IEntityTypeConfiguration<BlacklistedSsn>
{
    public void Configure(EntityTypeBuilder<BlacklistedSsn> builder)
    {
        builder.ToTable("BlacklistedSsns");
        builder.HasKey(b => b.Ssn);

        builder.Property(b => b.Ssn).HasMaxLength(9).IsRequired();
        builder.Property(b => b.Reason).HasMaxLength(200);

        // Seeded through the migration so a fresh clone has something to demonstrate against.
        // These are documented in the README as test data.
        builder.HasData(
            new BlacklistedSsn("666554444", "Known fraud ring"),
            new BlacklistedSsn("111111111", "Identity reported stolen"),
            new BlacklistedSsn("999999999", "Sanctions list match"));
    }
}
