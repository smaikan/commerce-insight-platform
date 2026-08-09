using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class GuestOrderSessionConfiguration : IEntityTypeConfiguration<GuestOrderSession>
{
    // Burada guest sipariş oturumunun hash, süre ve sorgu indekslerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<GuestOrderSession> builder)
    {
        builder.ToTable("GuestOrderSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(session => session.CsrfTokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(session => session.VerifiedEmailHash).HasMaxLength(64).IsFixedLength();
        builder.HasIndex(session => session.TokenHash).IsUnique();
        builder.HasIndex(session => new { session.ExpiresAt, session.RevokedAt });
        builder.HasIndex(session => session.VerifiedEmailHash);
    }
}
