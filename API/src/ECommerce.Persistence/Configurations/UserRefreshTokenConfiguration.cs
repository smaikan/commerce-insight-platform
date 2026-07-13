using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("UserRefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(token => token.CreatedByIp)
            .HasMaxLength(80);

        builder.Property(token => token.RevokedByIp)
            .HasMaxLength(80);

        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(120);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.ExpiresAt);
        builder.HasIndex(token => token.RevokedAt);
    }
}
