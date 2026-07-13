using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class FavoriteProductConfiguration : IEntityTypeConfiguration<FavoriteProduct>
{
    public void Configure(EntityTypeBuilder<FavoriteProduct> builder)
    {
        builder.ToTable("FavoriteProducts");

        builder.HasKey(favorite => favorite.Id);

        builder.HasOne(favorite => favorite.Product)
            .WithMany(product => product.Favorites)
            .HasForeignKey(favorite => favorite.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(favorite => new { favorite.ProductId, favorite.UserId })
            .IsUnique();
    }
}
