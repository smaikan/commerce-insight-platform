using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    // Burada varyantın ayrıştırılmış seçimlerini ve benzersizliklerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.ToTable("ProductVariantOptionValues");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id)
            .ValueGeneratedNever();
        builder.HasOne(item => item.ProductVariant)
            .WithMany(variant => variant.OptionValues)
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.VariantOptionValue)
            .WithMany()
            .HasForeignKey(item => item.VariantOptionValueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new
        {
            item.ProductVariantId,
            item.VariantOptionNameId
        })
            .IsUnique();
    }
}
