using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.Property(u => u.OriginalFileName).HasMaxLength(255);
        builder.Property(u => u.StoredFileName).HasMaxLength(100);
        builder.Property(u => u.FileName).HasMaxLength(255);
        builder.Property(u => u.ContentType).HasMaxLength(100);
    }
}
