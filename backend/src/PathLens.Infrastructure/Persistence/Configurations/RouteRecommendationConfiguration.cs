using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Routing;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class RouteRecommendationConfiguration : IEntityTypeConfiguration<RouteRecommendationRecord>
{
    public void Configure(EntityTypeBuilder<RouteRecommendationRecord> builder)
    {
        builder.ToTable("route_recommendations");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Profile).HasMaxLength(30).IsRequired();
        builder.Property(record => record.Provider).HasMaxLength(40).IsRequired();
        builder.HasIndex(record => record.CreatedAt);
    }
}
