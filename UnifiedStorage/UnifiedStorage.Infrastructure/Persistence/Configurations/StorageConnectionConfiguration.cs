using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedStorage.Domain.Entities;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Infrastructure.Persistence.Configurations;

public class StorageConnectionConfiguration : IEntityTypeConfiguration<StorageConnection>
{
    public void Configure(EntityTypeBuilder<StorageConnection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.EncryptedAccessToken)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(c => c.EncryptedRefreshToken)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(c => new { c.UserId, c.Provider })
            .IsUnique();

        builder.ToTable("StorageConnections");
    }
}
