using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(transaction => transaction.Reason)
            .HasMaxLength(500);

        builder.HasOne(transaction => transaction.ProductVariant)
            .WithMany(variant => variant.InventoryTransactions)
            .HasForeignKey(transaction => transaction.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(transaction => transaction.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transaction => transaction.ProductVariantId);
        builder.HasIndex(transaction => transaction.OrderId);
    }
}
