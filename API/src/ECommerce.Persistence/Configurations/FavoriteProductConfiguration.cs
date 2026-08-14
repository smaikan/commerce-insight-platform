using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class FavoriteProductConfiguration : IEntityTypeConfiguration<FavoriteProduct>
{
    // Burada favorinin kullanıcı veya guest session sahipliğini veritabanı invariantlarıyla koruyorum.
    public void Configure(EntityTypeBuilder<FavoriteProduct> builder)
    {
        builder.ToTable("FavoriteProducts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_FavoriteProducts_ExactlyOneOwner",
                """
                ([UserId] IS NOT NULL AND [SessionId] IS NULL)
                OR
                ([UserId] IS NULL AND [SessionId] IS NOT NULL AND [SessionId] <> '')
                """);
        });

        builder.HasKey(favorite => favorite.Id);

        builder.Property(favorite => favorite.SessionId)
            .HasMaxLength(FavoriteProduct.MaximumSessionIdLength);

        builder.HasOne(favorite => favorite.Product)
            .WithMany(product => product.Favorites)
            .HasForeignKey(favorite => favorite.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(favorite => new { favorite.UserId, favorite.ProductId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(favorite => new { favorite.SessionId, favorite.ProductId })
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL");
    }
}
