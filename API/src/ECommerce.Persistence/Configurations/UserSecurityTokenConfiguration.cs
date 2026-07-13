using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class UserSecurityTokenConfiguration : IEntityTypeConfiguration<UserSecurityToken>
{
    public void Configure(EntityTypeBuilder<UserSecurityToken> builder)
    {
        builder.ToTable("UserSecurityTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash);
        builder.HasIndex(token => new { token.Type, token.TokenHash });
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.ExpiresAt);
        builder.HasIndex(token => token.UsedAt);
    }
}
