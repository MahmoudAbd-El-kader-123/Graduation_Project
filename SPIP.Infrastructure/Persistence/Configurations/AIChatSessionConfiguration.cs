using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class AIChatSessionConfiguration : IEntityTypeConfiguration<AIChatSession>
{
    public void Configure(EntityTypeBuilder<AIChatSession> builder)
    {
        builder.Property(s => s.Title)
            .HasMaxLength(200);

        builder.HasIndex(s => s.UserId);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
