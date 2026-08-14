using ECommerce.Persistence.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductSearchGramConfiguration : IEntityTypeConfiguration<ProductSearchGram>
{
    // Burada gramdan ürüne hızlı aday araması sağlayan birleşik anahtarı yapılandırıyorum.
    public void Configure(EntityTypeBuilder<ProductSearchGram> builder)
    {
        builder.ToTable("ProductSearchGrams");
        builder.HasKey(gram => new { gram.Gram, gram.ProductId });
        builder.Property(gram => gram.Gram).HasMaxLength(3).IsRequired();
        builder.HasIndex(gram => gram.ProductId);
        builder.HasOne<ECommerce.Domain.Entities.Product>()
            .WithMany()
            .HasForeignKey(gram => gram.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
