using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(user => user.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Ignore(user => user.FullName);

        builder.HasMany(user => user.Addresses)
            .WithOne()
            .HasForeignKey(address => address.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.Carts)
            .WithOne()
            .HasForeignKey(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.CouponUsages)
            .WithOne()
            .HasForeignKey(usage => usage.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.FavoriteProducts)
            .WithOne()
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.Orders)
            .WithOne()
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.ProductRatings)
            .WithOne()
            .HasForeignKey(rating => rating.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.ProductReviews)
            .WithOne()
            .HasForeignKey(review => review.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.RefreshTokens)
            .WithOne(token => token.User)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.SecurityTokens)
            .WithOne(token => token.User)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasIndex(user => user.Status);
        builder.HasIndex(user => user.Role);
    }
}
