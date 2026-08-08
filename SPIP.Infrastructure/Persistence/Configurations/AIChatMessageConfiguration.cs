using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class AIChatMessageConfiguration : IEntityTypeConfiguration<AIChatMessage>
{
    public void Configure(EntityTypeBuilder<AIChatMessage> builder)
    {
        // ── Primary Key ───────────────────────────────────────────────────────
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()")
            .HasValueGenerator<SequentialGuidValueGenerator>()
            .ValueGeneratedOnAdd();

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.Role)
            .IsRequired();

        // ── Indexes ───────────────────────────────────────────────────────────
        // Composite index: retrieves messages for a session in chronological order.
        builder.HasIndex(m => new { m.SessionId, m.CreatedAt })
            .HasDatabaseName("IX_AIChatMessages_SessionId_CreatedAt");

        // ── Relationships ─────────────────────────────────────────────────────
        // Cascade: deleting a session removes all its messages. Orphaned messages have no meaning.
        builder.HasOne(m => m.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
