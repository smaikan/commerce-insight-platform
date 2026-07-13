using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductDailyMetricConfiguration : IEntityTypeConfiguration<ProductDailyMetric>
{
    public void Configure(EntityTypeBuilder<ProductDailyMetric> builder)
    {
        builder.ToTable("ProductDailyMetrics");

        builder.HasKey(metric => metric.Id);

        builder.HasOne(metric => metric.Product)
            .WithMany(product => product.DailyMetrics)
            .HasForeignKey(metric => metric.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(metric => new { metric.ProductId, metric.Date })
            .IsUnique();
    }
}
