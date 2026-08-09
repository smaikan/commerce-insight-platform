using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class GuestOrderMagicLinkConfiguration : IEntityTypeConfiguration<GuestOrderMagicLink>
{
    // Burada magic-link token hash'inin tekilliğini ve sipariş erişim sorgularını tanımlıyorum.
    public void Configure(EntityTypeBuilder<GuestOrderMagicLink> builder)
    {
        builder.ToTable("GuestOrderMagicLinks");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(link => link.EmailHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasOne(link => link.Order).WithMany().HasForeignKey(link => link.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(link => link.TokenHash).IsUnique();
        builder.HasIndex(link => new { link.OrderId, link.ExpiresAt });
    }
}
