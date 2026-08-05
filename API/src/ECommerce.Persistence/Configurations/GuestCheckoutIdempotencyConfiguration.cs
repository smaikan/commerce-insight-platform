using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class GuestCheckoutIdempotencyConfiguration : IEntityTypeConfiguration<GuestCheckoutIdempotency>
{
    // Burada guest checkout idempotency anahtarının cart kapsamındaki tekilliğini tanımlıyorum.
    public void Configure(EntityTypeBuilder<GuestCheckoutIdempotency> builder)
    {
        builder.ToTable("GuestCheckoutIdempotencies");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.CartSessionHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(record => record.KeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(record => record.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasOne(record => record.Order).WithMany().HasForeignKey(record => record.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(record => new { record.CartSessionHash, record.KeyHash }).IsUnique();
        builder.HasIndex(record => record.ExpiresAt);
    }
}
