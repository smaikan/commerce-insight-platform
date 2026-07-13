using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductRatingConfiguration : IEntityTypeConfiguration<ProductRating>
{
    public void Configure(EntityTypeBuilder<ProductRating> builder)
    {
        builder.ToTable("ProductRatings");

        builder.HasKey(rating => rating.Id);

        builder.HasOne(rating => rating.Product)
            .WithMany(product => product.Ratings)
            .HasForeignKey(rating => rating.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rating => new { rating.ProductId, rating.UserId })
            .IsUnique();
    }
}
