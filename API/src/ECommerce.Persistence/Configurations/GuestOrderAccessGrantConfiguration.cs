using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class GuestOrderAccessGrantConfiguration : IEntityTypeConfiguration<GuestOrderAccessGrant>
{
    // Burada session-sipariş erişim bağının benzersizliğini ve silme davranışlarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<GuestOrderAccessGrant> builder)
    {
        builder.ToTable("GuestOrderAccessGrants");
        builder.HasKey(grant => grant.Id);
        builder.HasOne(grant => grant.Session).WithMany().HasForeignKey(grant => grant.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(grant => grant.Order).WithMany().HasForeignKey(grant => grant.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(grant => new { grant.SessionId, grant.OrderId }).IsUnique();
        builder.HasIndex(grant => new { grant.OrderId, grant.RevokedAt });
    }
}
