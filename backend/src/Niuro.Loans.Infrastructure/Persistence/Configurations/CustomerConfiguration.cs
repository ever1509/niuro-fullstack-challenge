using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping lives here rather than as attributes on the entity, so the domain stays free of
/// any knowledge that a database exists.
/// </summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ssn)
            .HasConversion(ssn => ssn.Value, value => Ssn.Create(value))
            .HasMaxLength(9)
            .IsRequired();

        // One customer per SSN, enforced by the database and not only by the use case:
        // two concurrent submissions cannot both insert.
        builder.HasIndex(c => c.Ssn).IsUnique();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CompanyName).HasMaxLength(200).IsRequired();

        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("AddressStreet").HasMaxLength(200).IsRequired();
            address.Property(a => a.City).HasColumnName("AddressCity").HasMaxLength(100).IsRequired();
            address.Property(a => a.State).HasColumnName("AddressState").HasMaxLength(2).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("AddressPostalCode").HasMaxLength(10).IsRequired();
        });

        builder.Navigation(c => c.Address).IsRequired();
    }
}
