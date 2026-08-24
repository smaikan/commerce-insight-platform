using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class OrderCancellationOperationConfiguration : IEntityTypeConfiguration<OrderCancellationOperation>
{
    // Burada cancellation saga kolonlarını, concurrency tokenını ve tek aktif operasyon indeksini tanımlıyorum.
    public void Configure(EntityTypeBuilder<OrderCancellationOperation> builder)
    {
        builder.ToTable("OrderCancellationOperations", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_OrderCancellationOperations_AttemptCount_NonNegative", "[AttemptCount] >= 0");
        });
        builder.HasKey(operation => operation.Id);
        builder.Property(operation => operation.Id).ValueGeneratedNever();
        builder.Property(operation => operation.InitiatorType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(operation => operation.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(operation => operation.ReversalType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(operation => operation.ProviderConversationId)
            .HasMaxLength(OrderCancellationOperation.MaximumProviderConversationIdLength)
            .IsRequired();
        builder.Property(operation => operation.ProviderPaymentId)
            .HasMaxLength(OrderCancellationOperation.MaximumProviderPaymentIdLength)
            .IsRequired();
        builder.Property(operation => operation.ErrorCode).HasMaxLength(OrderCancellationOperation.MaximumErrorCodeLength);
        builder.Property(operation => operation.ErrorSummary).HasMaxLength(OrderCancellationOperation.MaximumErrorSummaryLength);
        builder.Property(operation => operation.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.HasOne(operation => operation.Order).WithMany().HasForeignKey(operation => operation.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(operation => operation.Payment).WithMany().HasForeignKey(operation => operation.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(operation => operation.Items).WithOne(item => item.Operation).HasForeignKey(item => item.OperationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(operation => operation.ProviderConversationId).IsUnique();
        builder.HasIndex(operation => new { operation.Status, operation.NextAttemptAt, operation.Id });
        builder.HasIndex(operation => operation.OrderId)
            .HasDatabaseName("UX_OrderCancellationOperations_ActiveOrder")
            .IsUnique()
            .HasFilter("[Status] IN ('Requested', 'Processing', 'ReconciliationPending')");
    }
}

public sealed class OrderCancellationOperationItemConfiguration : IEntityTypeConfiguration<OrderCancellationOperationItem>
{
    // Burada item-level refund audit kayıtlarının provider kimliği ve tutar bütünlüğünü tanımlıyorum.
    public void Configure(EntityTypeBuilder<OrderCancellationOperationItem> builder)
    {
        builder.ToTable("OrderCancellationOperationItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_OrderCancellationOperationItems_Amount_Positive", "[Amount] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(item => item.ProviderPaymentTransactionId)
            .HasMaxLength(OrderCancellationOperationItem.MaximumProviderTransactionIdLength)
            .IsRequired();
        builder.Property(item => item.ProviderConversationId)
            .HasMaxLength(OrderCancellationOperationItem.MaximumProviderConversationIdLength)
            .IsRequired();
        builder.Property(item => item.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.ErrorCode).HasMaxLength(OrderCancellationOperationItem.MaximumErrorCodeLength);
        builder.HasOne(item => item.PaymentItemTransaction).WithMany().HasForeignKey(item => item.PaymentItemTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.ProviderConversationId).IsUnique();
        builder.HasIndex(item => new { item.OperationId, item.ProviderPaymentTransactionId }).IsUnique();
    }
}
