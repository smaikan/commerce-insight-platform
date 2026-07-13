using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.ToTable("ProductTags");

        builder.HasKey(productTag => productTag.Id);

        builder.HasOne(productTag => productTag.Product)
            .WithMany(product => product.ProductTags)
            .HasForeignKey(productTag => productTag.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(productTag => productTag.Tag)
            .WithMany(tag => tag.ProductTags)
            .HasForeignKey(productTag => productTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(productTag => new { productTag.ProductId, productTag.TagId })
            .IsUnique();
    }
}
