using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ECommerce.Persistence.Configurations;
public sealed class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    // Burada varyantın ayrıştırılmış seçimlerini ve benzersizliklerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.ToTable("ProductVariantOptionValues"); builder.HasKey(x => x.Id);
        builder.HasOne(x => x.ProductVariant).WithMany(x => x.OptionValues).HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.VariantOptionValue).WithMany().HasForeignKey(x => x.VariantOptionValueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProductVariantId, x.VariantOptionNameId }).IsUnique();
    }
}
