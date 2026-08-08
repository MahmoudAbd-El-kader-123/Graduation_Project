using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class AIChatSessionConfiguration : IEntityTypeConfiguration<AIChatSession>
{
    public void Configure(EntityTypeBuilder<AIChatSession> builder)
    {
        // ── Primary Key ───────────────────────────────────────────────────────
        // Application generates the Guid (SequentialGuidValueGenerator → SQL Server-optimised ordering).
        // NEWSEQUENTIALID() is set as the DB default for scenarios where the DB inserts directly.
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()")
            .HasValueGenerator<SequentialGuidValueGenerator>()
            .ValueGeneratedOnAdd();

        // ── Properties ────────────────────────────────────────────────────────
        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.IsArchived)
            .IsRequired();

        // ── Indexes ───────────────────────────────────────────────────────────

        // Single-column index: filters all sessions for a user
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_AIChatSessions_UserId");

        // Composite index: ordered session list per user (ORDER BY UpdatedAt DESC is the common query)
        builder.HasIndex(s => new { s.UserId, s.UpdatedAt })
            .HasDatabaseName("IX_AIChatSessions_UserId_UpdatedAt");

        // ── Relationships ─────────────────────────────────────────────────────
        // Restrict: do not cascade-delete sessions when a user is removed.
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
