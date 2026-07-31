using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    // Burada sepet tablosunun sahiplik, ilişki, indeks ve concurrency kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Carts_ExactlyOneOwner",
                """
                ([UserId] IS NOT NULL AND [SessionId] IS NULL)
                OR
                ([UserId] IS NULL AND [SessionId] IS NOT NULL AND [SessionId] <> '')
                """);
        });

        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.SessionId)
            .HasMaxLength(120);

        builder.Property(cart => cart.ConcurrencyToken)
            .IsConcurrencyToken();

        builder.HasMany(cart => cart.Items)
            .WithOne(item => item.Cart)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cart => cart.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(cart => cart.SessionId)
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL");
    }
}
