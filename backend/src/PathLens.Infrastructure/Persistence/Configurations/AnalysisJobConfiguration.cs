using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Evidence;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        builder.ToTable("analysis_jobs");
        builder.HasKey(job => job.Id);

        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(job => job.ErrorMessage).HasMaxLength(2000);

        builder.HasOne<EvidenceItem>()
            .WithMany()
            .HasForeignKey(job => job.EvidenceItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(job => new { job.Status, job.CreatedAt });
        builder.HasIndex(job => job.EvidenceItemId);
    }
}
