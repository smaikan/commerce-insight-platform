using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductVariantDailyMetricConfiguration : IEntityTypeConfiguration<ProductVariantDailyMetric>
{
    // Burada günlük varyant sayaçlarını bigint kolonlarıyla ve tekil gün indeksiyle eşliyorum.
    public void Configure(EntityTypeBuilder<ProductVariantDailyMetric> builder)
    {
        builder.ToTable("ProductVariantDailyMetrics");

        builder.HasKey(metric => metric.Id);

        builder.HasOne(metric => metric.ProductVariant)
            .WithMany(variant => variant.DailyMetrics)
            .HasForeignKey(metric => metric.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(metric => new { metric.ProductVariantId, metric.Date })
            .IsUnique();
    }
}
