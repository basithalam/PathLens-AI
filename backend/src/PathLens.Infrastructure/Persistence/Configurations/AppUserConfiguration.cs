using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathLens.Domain.Users;

namespace PathLens.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
    }
}
