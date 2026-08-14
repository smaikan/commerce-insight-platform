using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    // Burada ürün tablosunun kolon, ilişki ve indeks kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table => table.UseSqlOutputClause(false));

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .ValueGeneratedOnAdd();

        builder.Property(product => product.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(product => product.MainSku)
            .HasMaxLength(Product.MaximumMainSkuLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(4000);

        builder.Property(product => product.Url)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(product => product.SeoTitle)
            .HasMaxLength(250);

        builder.Property(product => product.SeoDescription)
            .HasMaxLength(500);

        builder.Property(product => product.HasVariants)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(product => product.AverageRating)
            .HasPrecision(3, 2);

        builder.Property(product => product.PopularityScore)
            .IsRequired();

        builder.Property(product => product.ConcurrencyToken)
            .IsConcurrencyToken();

        builder.Property(product => product.DeletedAtUtc);

        builder.HasOne(product => product.Type)
            .WithMany(type => type.Products)
            .HasForeignKey(product => product.TypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(product => product.Brand)
            .WithMany(brand => brand.Products)
            .HasForeignKey(product => product.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(product => product.TaxRate)
            .WithMany(taxRate => taxRate.Products)
            .HasForeignKey(product => product.TaxRateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(product => product.Variants)
            .WithOne(variant => variant.Product)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Images)
            .WithOne(image => image.Product)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.UrlRedirects)
            .WithOne(redirect => redirect.Product)
            .HasForeignKey(redirect => redirect.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.ProductCollections)
            .WithOne(productCollection => productCollection.Product)
            .HasForeignKey(productCollection => productCollection.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.ProductTags)
            .WithOne(productTag => productTag.Product)
            .HasForeignKey(productTag => productTag.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.DailyMetrics)
            .WithOne(metric => metric.Product)
            .HasForeignKey(metric => metric.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Ratings)
            .WithOne(rating => rating.Product)
            .HasForeignKey(rating => rating.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Reviews)
            .WithOne(review => review.Product)
            .HasForeignKey(review => review.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Favorites)
            .WithOne(favorite => favorite.Product)
            .HasForeignKey(favorite => favorite.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.BundleItems)
            .WithOne(bundleItem => bundleItem.BundleProduct)
            .HasForeignKey(bundleItem => bundleItem.BundleProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(product => product.Url)
            .IsUnique()
            .HasFilter("[DeletedAtUtc] IS NULL");

        builder.HasIndex(product => product.MainSku)
            .IsUnique()
            .HasFilter("[DeletedAtUtc] IS NULL");

        builder.HasIndex(product => product.TypeId);
        builder.HasIndex(product => product.BrandId);
        builder.HasIndex(product => product.TaxRateId);
        builder.HasIndex(product => product.Status);
        builder.HasIndex(product => product.DisplayOrder);
        builder.HasIndex(product => product.PopularityScore);
        builder.HasIndex(product => product.DeletedAtUtc);
    }
}
