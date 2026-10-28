using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Cameras;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class CameraSourceConfiguration : IEntityTypeConfiguration<CameraSource>
{
    public void Configure(EntityTypeBuilder<CameraSource> builder)
    {
        builder.ToTable("camera_sources");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.Provider).HasConversion<string>().HasMaxLength(30);
        builder.Property(source => source.ExternalId).HasMaxLength(120).IsRequired();
        builder.Property(source => source.Label).HasMaxLength(200).IsRequired();
        builder.Property(source => source.City).HasMaxLength(120);
        builder.Property(source => source.FeedImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(source => source.DirectionLabel).HasMaxLength(120);
        builder.Property(source => source.PermissionNote).HasMaxLength(2000);
        builder.Property(source => source.LastError).HasMaxLength(2000);
        builder.Property(source => source.LastImageHash).HasMaxLength(64);

        builder.HasIndex(source => new { source.Provider, source.ExternalId }).IsUnique();
        builder.HasIndex(source => source.Enabled);
    }
}
