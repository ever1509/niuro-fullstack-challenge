using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Loans.Infrastructure.Outbox;

namespace Niuro.Loans.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).IsRequired();

        // The dispatcher's only query: undelivered messages that are due. Indexing both
        // columns keeps that poll cheap as the table grows.
        builder.HasIndex(m => new { m.ProcessedAtUtc, m.NextAttemptAtUtc });
    }
}
