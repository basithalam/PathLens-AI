using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Reports;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class AccessibilityReportConfiguration : IEntityTypeConfiguration<AccessibilityReport>
{
    public void Configure(EntityTypeBuilder<AccessibilityReport> builder)
    {
        builder.ToTable("accessibility_reports");
        builder.HasKey(report => report.Id);

        builder.Property(report => report.Title).HasMaxLength(200).IsRequired();
        builder.Property(report => report.Description).HasMaxLength(2000);
        builder.Property(report => report.ObstacleType).HasConversion<string>().HasMaxLength(40);
        builder.Property(report => report.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(report => report.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(report => report.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(report => report.Surface).HasConversion<string>().HasMaxLength(20);
        builder.Property(report => report.AttributeSource).HasMaxLength(20);

        builder.HasIndex(report => report.ObstacleType);
        builder.HasIndex(report => report.VerificationStatus);
        builder.HasIndex(report => report.CreatedAt);
        builder.HasIndex(report => new { report.Latitude, report.Longitude });
        builder.HasIndex(report => report.CameraSourceId);
        builder.HasIndex(report => report.PossibleDuplicateOfReportId);
        builder.HasIndex(report => report.DuplicateOfReportId);
    }
}
