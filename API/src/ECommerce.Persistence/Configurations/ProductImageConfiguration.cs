using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(image => image.AltText)
            .HasMaxLength(250);

        builder.Property(image => image.ConcurrencyToken)
            .IsConcurrencyToken();

        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(image => new { image.ProductId, image.DisplayOrder });

        builder.HasIndex(image => image.ProductId)
            .IsUnique()
            .HasFilter("[IsMain] = 1");
    }
}
