using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class StorefrontBannerConfiguration : IEntityTypeConfiguration<StorefrontBanner>
{
    // Burada banner tablosunun bölüm, medya, sıralama ve benzersizlik kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<StorefrontBanner> builder)
    {
        builder.ToTable("StorefrontBanners", table =>
        {
            table.HasCheckConstraint(
                "CK_StorefrontBanners_DisplayOrder",
                "[DisplayOrder] >= 0");
            table.HasCheckConstraint(
                "CK_StorefrontBanners_IsMainSection",
                "[IsMain] = 0 OR [Section] = 0");
            table.HasCheckConstraint(
                "CK_StorefrontBanners_MediaType",
                "[MediaType] IN (1, 2)");
        });

        builder.HasKey(banner => banner.Id);

        builder.Property(banner => banner.Section)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(banner => banner.Name)
            .HasMaxLength(StorefrontBanner.MaximumNameLength)
            .IsRequired();

        builder.Property(banner => banner.Key)
            .HasMaxLength(StorefrontBanner.MaximumKeyLength)
            .IsRequired();

        builder.Property(banner => banner.MediaUrl)
            .HasMaxLength(StorefrontBanner.MaximumUrlLength)
            .IsRequired();

        builder.Property(banner => banner.MediaType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(banner => banner.TargetUrl)
            .HasMaxLength(StorefrontBanner.MaximumUrlLength);

        builder.Property(banner => banner.AltText)
            .HasMaxLength(StorefrontBanner.MaximumAltTextLength);

        builder.Property(banner => banner.DisplayOrder)
            .IsRequired();

        builder.Property(banner => banner.IsActive)
            .IsRequired();

        builder.Property(banner => banner.IsMain)
            .IsRequired();

        builder.HasIndex(banner => new { banner.Section, banner.Key })
            .IsUnique();

        builder.HasIndex(banner => new { banner.Section, banner.IsActive, banner.DisplayOrder });
    }
}
