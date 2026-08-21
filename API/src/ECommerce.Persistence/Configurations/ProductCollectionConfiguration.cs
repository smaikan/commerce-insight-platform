using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductCollectionConfiguration : IEntityTypeConfiguration<ProductCollection>
{
    public void Configure(EntityTypeBuilder<ProductCollection> builder)
    {
        builder.ToTable("ProductCollections", table => table.UseSqlOutputClause(false));

        builder.HasKey(productCollection => productCollection.Id);

        builder.HasOne(productCollection => productCollection.Product)
            .WithMany(product => product.ProductCollections)
            .HasForeignKey(productCollection => productCollection.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(productCollection => productCollection.Collection)
            .WithMany(collection => collection.ProductCollections)
            .HasForeignKey(productCollection => productCollection.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(productCollection => new { productCollection.ProductId, productCollection.CollectionId })
            .IsUnique();
    }
}
