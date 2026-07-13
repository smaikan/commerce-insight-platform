using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Title)
            .HasMaxLength(200);

        builder.Property(review => review.Comment)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasOne(review => review.Product)
            .WithMany(product => product.Reviews)
            .HasForeignKey(review => review.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(review => review.ProductId);
        builder.HasIndex(review => review.UserId);
        builder.HasIndex(review => review.IsApproved);
    }
}
