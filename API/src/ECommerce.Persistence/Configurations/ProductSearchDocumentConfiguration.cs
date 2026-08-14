using ECommerce.Persistence.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductSearchDocumentConfiguration : IEntityTypeConfiguration<ProductSearchDocument>
{
    // Burada ürün başına tek arama dokümanının kolon ve indeks sınırlarını yapılandırıyorum.
    public void Configure(EntityTypeBuilder<ProductSearchDocument> builder)
    {
        builder.ToTable("ProductSearchDocuments");
        builder.HasKey(document => document.ProductId);
        builder.Property(document => document.TitleNormalized).HasMaxLength(250).IsRequired();
        builder.Property(document => document.BrandNormalized).HasMaxLength(150).IsRequired();
        builder.Property(document => document.TypeNormalized).HasMaxLength(150).IsRequired();
        builder.Property(document => document.CollectionNamesNormalized).HasMaxLength(2000).IsRequired();
        builder.Property(document => document.TagNamesNormalized).HasMaxLength(2000).IsRequired();
        builder.Property(document => document.MainSkuNormalized).HasMaxLength(100).IsRequired();
        builder.Property(document => document.SearchTextNormalized).HasMaxLength(4000).IsRequired();
        builder.HasIndex(document => new { document.TitleNormalized, document.ProductId });
        builder.HasIndex(document => new { document.BrandNormalized, document.ProductId });
        builder.HasIndex(document => new { document.TypeNormalized, document.ProductId });
        builder.HasIndex(document => new { document.MainSkuNormalized, document.ProductId });
        builder.HasOne<ECommerce.Domain.Entities.Product>()
            .WithOne()
            .HasForeignKey<ProductSearchDocument>(document => document.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
