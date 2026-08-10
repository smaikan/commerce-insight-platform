using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class StorefrontBannerConfiguration : IEntityTypeConfiguration<StorefrontBanner>
{
    // Burada storefront banner tablosunun sabit alan ve URL kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<StorefrontBanner> builder)
    {
        builder.ToTable("StorefrontBanners");

        builder.HasKey(banner => banner.Id);

        builder.Property(banner => banner.Slot)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(banner => banner.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(banner => banner.Slot)
            .IsUnique();
    }
}
