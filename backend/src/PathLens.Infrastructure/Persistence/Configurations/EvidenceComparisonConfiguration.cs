using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class EvidenceComparisonConfiguration : IEntityTypeConfiguration<EvidenceComparison>
{
    public void Configure(EntityTypeBuilder<EvidenceComparison> builder)
    {
        builder.ToTable("evidence_comparisons");
        builder.HasKey(comparison => comparison.Id);

        builder.Property(comparison => comparison.Outcome).HasConversion<string>().HasMaxLength(30);
        builder.Property(comparison => comparison.ReviewedOutcome).HasConversion<string>().HasMaxLength(30);
        builder.Property(comparison => comparison.Explanation).HasMaxLength(4000).IsRequired();
        builder.Property(comparison => comparison.ReviewReason).HasMaxLength(2000);
        builder.Property(comparison => comparison.ModelVersion).HasMaxLength(120);

        builder.HasOne<AccessibilityReport>()
            .WithMany()
            .HasForeignKey(comparison => comparison.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EvidenceItem>()
            .WithMany()
            .HasForeignKey(comparison => comparison.EarlierEvidenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<EvidenceItem>()
            .WithMany()
            .HasForeignKey(comparison => comparison.LaterEvidenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(comparison => comparison.ReportId);
        builder.HasIndex(comparison => comparison.CreatedAt);
    }
}
