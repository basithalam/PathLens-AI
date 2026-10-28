using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Users;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class ReviewAuditEventConfiguration : IEntityTypeConfiguration<ReviewAuditEvent>
{
    public void Configure(EntityTypeBuilder<ReviewAuditEvent> builder)
    {
        builder.ToTable("review_audit_events");
        builder.HasKey(auditEvent => auditEvent.Id);
        builder.Property(auditEvent => auditEvent.EntityType).HasMaxLength(40).IsRequired();
        builder.Property(auditEvent => auditEvent.Action).HasMaxLength(60).IsRequired();
        builder.Property(auditEvent => auditEvent.PreviousValue).HasMaxLength(60);
        builder.Property(auditEvent => auditEvent.NewValue).HasMaxLength(60);
        builder.Property(auditEvent => auditEvent.Note).HasMaxLength(2000);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(auditEvent => new { auditEvent.EntityType, auditEvent.EntityId });
        builder.HasIndex(auditEvent => auditEvent.CreatedAt);
    }
}
