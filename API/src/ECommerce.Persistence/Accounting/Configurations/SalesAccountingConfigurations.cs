using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Accounting.Configurations;

public sealed class AccountingSalesOrderConfiguration :
    IEntityTypeConfiguration<AccountingSalesOrder>
{
    // Burada Accounting satış siparişinin başlık, toplam, tekillik ve ilişki kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<AccountingSalesOrder> builder)
    {
        builder.ToTable("AccountingSalesOrders");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.IdempotencyKey)
            .HasMaxLength(AccountingSalesOrder.MaximumIdempotencyKeyLength)
            .IsRequired();
        builder.Property(item => item.OrderNumber)
            .HasMaxLength(AccountingSalesOrder.MaximumOrderNumberLength)
            .IsRequired();
        builder.Property(item => item.CurrentAccountNameSnapshot)
            .HasMaxLength(CurrentAccount.MaximumNameLength)
            .IsRequired();
        builder.Property(item => item.TaxNumberSnapshot)
            .HasMaxLength(CurrentAccount.MaximumIdentityNumberLength);
        builder.Property(item => item.TaxOfficeSnapshot)
            .HasMaxLength(CurrentAccount.MaximumTaxOfficeLength);
        builder.Property(item => item.PhoneNumberSnapshot)
            .HasMaxLength(CurrentAccount.MaximumPhoneLength);
        builder.Property(item => item.EmailSnapshot)
            .HasMaxLength(CurrentAccount.MaximumEmailLength);
        builder.Property(item => item.AddressSnapshot)
            .HasMaxLength(AccountingSalesOrder.MaximumAddressSnapshotLength);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(item => item.ExchangeRate).HasPrecision(18, 6);
        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(item => item.ShippingPayer)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(item => item.Description)
            .HasMaxLength(AccountingSalesOrder.MaximumDescriptionLength);
        builder.Property(item => item.CancellationReason)
            .HasMaxLength(AccountingSalesOrder.MaximumDescriptionLength);
        builder.Property(item => item.InvoiceDiscountType)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.InvoiceDiscountTaxBasis)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.InvoiceDiscountValue).HasPrecision(18, 4);
        ConfigureMoney(builder);
        builder.HasOne(item => item.CurrentAccount)
            .WithMany()
            .HasForeignKey(item => item.CurrentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Items)
            .WithOne(item => item.AccountingSalesOrder)
            .HasForeignKey(item => item.AccountingSalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.SalesInvoice)
            .WithOne(item => item.AccountingSalesOrder)
            .HasForeignKey<SalesInvoice>(item => item.AccountingSalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.HasIndex(item => item.OrderNumber).IsUnique();
        builder.HasIndex(item => new { item.Status, item.OrderDate, item.Id });
    }

    // Burada satış siparişinin bütün parasal ve kârlılık alanlarını ortak hassasiyete bağlıyorum.
    private static void ConfigureMoney(EntityTypeBuilder<AccountingSalesOrder> builder)
    {
        builder.Property(item => item.SubtotalExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.SubtotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountTotalExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountTotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountTotalExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountTotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.NetAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.ShippingTotal).HasPrecision(18, 2);
        builder.Property(item => item.VatTotal).HasPrecision(18, 2);
        builder.Property(item => item.GrandTotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.PaidAmount).HasPrecision(18, 2);
        builder.Property(item => item.RemainingAmount).HasPrecision(18, 2);
        builder.Property(item => item.TotalCostOfGoodsSold).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitMargin).HasPrecision(9, 4);
    }
}

public sealed class AccountingSalesOrderItemConfiguration :
    IEntityTypeConfiguration<AccountingSalesOrderItem>
{
    // Burada satış siparişi satırının ürün snapshot, miktar, hesap ve etki ilişkilerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<AccountingSalesOrderItem> builder)
    {
        builder.ToTable("AccountingSalesOrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingSalesOrderItems_Quantity",
                "[Quantity] > 0 AND [UnitsPerSaleUnit] > 0 AND [StockQuantity] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductNameSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumProductNameLength)
            .IsRequired();
        builder.Property(item => item.VariantNameSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumVariantNameLength)
            .IsRequired();
        builder.Property(item => item.SkuSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumSkuLength)
            .IsRequired();
        builder.Property(item => item.BarcodeSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumBarcodeLength);
        builder.Property(item => item.UnitOfMeasure)
            .HasMaxLength(AccountingSalesOrderItem.MaximumUnitOfMeasureLength)
            .IsRequired();
        builder.Property(item => item.Quantity).HasPrecision(18, 4);
        builder.Property(item => item.UnitsPerSaleUnit).HasPrecision(18, 4);
        builder.Property(item => item.EnteredUnitPrice).HasPrecision(18, 4);
        builder.Property(item => item.UnitPriceExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.UnitPriceIncludingVat).HasPrecision(18, 4);
        builder.Property(item => item.VatRate).HasPrecision(9, 4);
        builder.Property(item => item.PriceEntryMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(item => item.LineDiscountType)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.LineDiscountTaxBasis)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.LineDiscountUnitBasis)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.LineDiscountValue).HasPrecision(18, 4);
        ConfigureMoney(builder);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.StockMovements)
            .WithOne(item => item.AccountingSalesOrderItem)
            .HasForeignKey(item => item.AccountingSalesOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.CostLayerConsumptions)
            .WithOne(item => item.AccountingSalesOrderItem)
            .HasForeignKey(item => item.AccountingSalesOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.StockMovements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.CostLayerConsumptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(item => new { item.AccountingSalesOrderId, item.LineNumber })
            .IsUnique();
        builder.HasIndex(item => item.ProductVariantId);
    }

    // Burada satış satırının bütün parasal ve kârlılık alanlarını ortak hassasiyete bağlıyorum.
    private static void ConfigureMoney(EntityTypeBuilder<AccountingSalesOrderItem> builder)
    {
        builder.Property(item => item.GrossAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.GrossAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountShareExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountShareIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.NetAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.VatAmount).HasPrecision(18, 2);
        builder.Property(item => item.TotalAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.CostOfGoodsSold).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitMargin).HasPrecision(9, 4);
    }
}

public sealed class AccountingSalesOrderStockMovementConfiguration :
    IEntityTypeConfiguration<AccountingSalesOrderStockMovement>
{
    // Burada Accounting satırı ile mevcut stok hareketi arasındaki değişmez bire bir bağı tanımlıyorum.
    public void Configure(EntityTypeBuilder<AccountingSalesOrderStockMovement> builder)
    {
        builder.ToTable("AccountingSalesOrderStockMovements", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingSalesOrderStockMovements_Quantity",
                "[Quantity] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasOne(item => item.StockMovement)
            .WithMany()
            .HasForeignKey(item => item.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.AccountingSalesOrderItemId).IsUnique();
        builder.HasIndex(item => item.StockMovementId).IsUnique();
    }
}

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    // Burada iç satış faturasının sipariş bağı, snapshot, toplam ve numara tekilliğini tanımlıyorum.
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("AccountingSalesInvoices");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CurrentAccountNameSnapshot)
            .HasMaxLength(CurrentAccount.MaximumNameLength)
            .IsRequired();
        builder.Property(item => item.TaxNumberSnapshot)
            .HasMaxLength(CurrentAccount.MaximumIdentityNumberLength);
        builder.Property(item => item.TaxOfficeSnapshot)
            .HasMaxLength(CurrentAccount.MaximumTaxOfficeLength);
        builder.Property(item => item.PhoneNumberSnapshot)
            .HasMaxLength(CurrentAccount.MaximumPhoneLength);
        builder.Property(item => item.EmailSnapshot)
            .HasMaxLength(CurrentAccount.MaximumEmailLength);
        builder.Property(item => item.AddressSnapshot)
            .HasMaxLength(SalesInvoice.MaximumAddressSnapshotLength);
        builder.Property(item => item.InvoiceNumber)
            .HasMaxLength(SalesInvoice.MaximumInvoiceNumberLength)
            .IsRequired();
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(item => item.ExchangeRate).HasPrecision(18, 6);
        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(item => item.ShippingPayer)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(item => item.Description)
            .HasMaxLength(SalesInvoice.MaximumDescriptionLength);
        builder.Property(item => item.CancellationReason)
            .HasMaxLength(SalesInvoice.MaximumDescriptionLength);
        builder.Property(item => item.InvoiceDiscountType)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.InvoiceDiscountTaxBasis)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.InvoiceDiscountValue).HasPrecision(18, 4);
        ConfigureMoney(builder);
        builder.HasOne(item => item.CurrentAccount)
            .WithMany()
            .HasForeignKey(item => item.CurrentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Lines)
            .WithOne(item => item.SalesInvoice)
            .HasForeignKey(item => item.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(item => item.AccountingSalesOrderId).IsUnique();
        builder.HasIndex(item => new { item.CurrentAccountId, item.InvoiceNumber }).IsUnique();
        builder.HasIndex(item => new { item.Status, item.InvoiceDate, item.Id });
    }

    // Burada satış faturasının bütün parasal ve kârlılık alanlarını ortak hassasiyete bağlıyorum.
    private static void ConfigureMoney(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.Property(item => item.SubtotalExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.SubtotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountTotalExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountTotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountTotalExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountTotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.NetAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.ShippingTotal).HasPrecision(18, 2);
        builder.Property(item => item.VatTotal).HasPrecision(18, 2);
        builder.Property(item => item.GrandTotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.PaidAmount).HasPrecision(18, 2);
        builder.Property(item => item.RemainingAmount).HasPrecision(18, 2);
        builder.Property(item => item.TotalCostOfGoodsSold).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitMargin).HasPrecision(9, 4);
    }
}

public sealed class SalesInvoiceLineConfiguration :
    IEntityTypeConfiguration<SalesInvoiceLine>
{
    // Burada satış faturası satırının sipariş satırı bağı, snapshot ve hesap alanlarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.ToTable("AccountingSalesInvoiceLines", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingSalesInvoiceLines_Quantity",
                "[Quantity] > 0 AND [UnitsPerSaleUnit] > 0 AND [StockQuantity] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductNameSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumProductNameLength)
            .IsRequired();
        builder.Property(item => item.VariantNameSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumVariantNameLength)
            .IsRequired();
        builder.Property(item => item.SkuSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumSkuLength)
            .IsRequired();
        builder.Property(item => item.BarcodeSnapshot)
            .HasMaxLength(AccountingSalesOrderItem.MaximumBarcodeLength);
        builder.Property(item => item.UnitOfMeasure)
            .HasMaxLength(AccountingSalesOrderItem.MaximumUnitOfMeasureLength)
            .IsRequired();
        builder.Property(item => item.Quantity).HasPrecision(18, 4);
        builder.Property(item => item.UnitsPerSaleUnit).HasPrecision(18, 4);
        builder.Property(item => item.EnteredUnitPrice).HasPrecision(18, 4);
        builder.Property(item => item.UnitPriceExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.UnitPriceIncludingVat).HasPrecision(18, 4);
        builder.Property(item => item.VatRate).HasPrecision(9, 4);
        builder.Property(item => item.PriceEntryMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(item => item.LineDiscountType)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.LineDiscountTaxBasis)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.LineDiscountUnitBasis)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(item => item.LineDiscountValue).HasPrecision(18, 4);
        ConfigureMoney(builder);
        builder.HasOne(item => item.AccountingSalesOrderItem)
            .WithMany()
            .HasForeignKey(item => item.AccountingSalesOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.AccountingSalesOrderItemId).IsUnique();
        builder.HasIndex(item => new { item.SalesInvoiceId, item.LineNumber }).IsUnique();
        builder.HasIndex(item => item.ProductVariantId);
    }

    // Burada fatura satırının bütün parasal ve kârlılık alanlarını ortak hassasiyete bağlıyorum.
    private static void ConfigureMoney(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.Property(item => item.GrossAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.GrossAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.LineDiscountAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountShareExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.InvoiceDiscountShareIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalDiscountAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.NetAmountExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.VatAmount).HasPrecision(18, 2);
        builder.Property(item => item.TotalAmountIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.CostOfGoodsSold).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.GrossProfitMargin).HasPrecision(9, 4);
    }
}

public sealed class CostLayerConsumptionConfiguration :
    IEntityTypeConfiguration<CostLayerConsumption>
{
    // Burada FIFO tüketiminin katman, satış satırı ve gerçek stok hareketi bağlarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<CostLayerConsumption> builder)
    {
        builder.ToTable("AccountingCostLayerConsumptions", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingCostLayerConsumptions_Quantity",
                "[Quantity] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.UnitCostExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.TotalCostExcludingVat).HasPrecision(18, 2);
        builder.HasOne(item => item.InventoryCostLayer)
            .WithMany(item => item.Consumptions)
            .HasForeignKey(item => item.InventoryCostLayerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.StockMovement)
            .WithMany()
            .HasForeignKey(item => item.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new
        {
            item.InventoryCostLayerId,
            item.AccountingSalesOrderItemId,
            item.StockMovementId
        }).IsUnique();
        builder.HasIndex(item => new
        {
            item.AccountingSalesOrderItemId,
            item.CreatedAt,
            item.Id
        });
        builder.HasIndex(item => item.StockMovementId);
    }
}
