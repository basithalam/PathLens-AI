using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class EvidenceItemConfiguration : IEntityTypeConfiguration<EvidenceItem>
{
    public void Configure(EntityTypeBuilder<EvidenceItem> builder)
    {
        builder.ToTable("evidence_items");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.StorageKey).HasMaxLength(120).IsRequired();
        builder.Property(item => item.FileName).HasMaxLength(255).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(60).IsRequired();
        builder.Property(item => item.SourceType).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.AnalysisStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.AnalysisProvider).HasMaxLength(40);
        builder.Property(item => item.AnalysisModel).HasMaxLength(120);

        builder.HasOne<AccessibilityReport>()
            .WithMany()
            .HasForeignKey(item => item.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.ReportId);
        builder.HasIndex(item => item.CameraSourceId);
        builder.HasIndex(item => item.AnalysisStatus);
    }
}
