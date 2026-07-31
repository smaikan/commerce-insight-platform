using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Accounting.Configurations;

internal static class AccountingConfigurationMarker
{
    // Burada tedarikçi ana verisinin kolonlarını ve benzersiz kodunu tanımlıyorum.
}

public sealed class CurrentAccountConfiguration : IEntityTypeConfiguration<CurrentAccount>
{
    // Burada supplier ve para birimi bazındaki cari hesap tekilliğini tanımlıyorum.
    public void Configure(EntityTypeBuilder<CurrentAccount> builder)
    {
        builder.ToTable("AccountingCurrentAccounts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.Code).HasMaxLength(CurrentAccount.MaximumCodeLength).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(CurrentAccount.MaximumNameLength).IsRequired();
        builder.Property(item => item.TradeName).HasMaxLength(CurrentAccount.MaximumTradeNameLength);
        builder.Property(item => item.NationalIdentityNumber).HasMaxLength(CurrentAccount.MaximumIdentityNumberLength);
        builder.Property(item => item.TaxNumber).HasMaxLength(CurrentAccount.MaximumIdentityNumberLength);
        builder.Property(item => item.TaxOffice).HasMaxLength(CurrentAccount.MaximumTaxOfficeLength);
        builder.Property(item => item.PhoneNumber).HasMaxLength(CurrentAccount.MaximumPhoneLength);
        builder.Property(item => item.Email).HasMaxLength(CurrentAccount.MaximumEmailLength);
        builder.Property(item => item.Country).HasMaxLength(CurrentAccount.MaximumAddressPartLength);
        builder.Property(item => item.City).HasMaxLength(CurrentAccount.MaximumAddressPartLength);
        builder.Property(item => item.District).HasMaxLength(CurrentAccount.MaximumAddressPartLength);
        builder.Property(item => item.Neighborhood).HasMaxLength(CurrentAccount.MaximumAddressPartLength);
        builder.Property(item => item.AddressLine).HasMaxLength(CurrentAccount.MaximumAddressLineLength);
        builder.Property(item => item.PostalCode).HasMaxLength(CurrentAccount.MaximumPostalCodeLength);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Transactions)
            .WithOne(item => item.CurrentAccount)
            .HasForeignKey(item => item.CurrentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.Code).IsUnique();
        builder.HasIndex(item => item.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
    }
}

public sealed class CurrentAccountTransactionConfiguration : IEntityTypeConfiguration<CurrentAccountTransaction>
{
    // Burada değişmez cari hareketin tutar, kaynak ve idempotency kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<CurrentAccountTransaction> builder)
    {
        builder.ToTable("AccountingCurrentAccountTransactions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(item => item.SourceType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(item => item.DebitAmount).HasPrecision(18, 2);
        builder.Property(item => item.CreditAmount).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(item => item.ExchangeRate).HasPrecision(18, 6);
        builder.Property(item => item.Description).HasMaxLength(CurrentAccountTransaction.MaximumDescriptionLength);
        builder.HasIndex(item => new { item.SourceType, item.SourceId, item.Type }).IsUnique();
        builder.HasIndex(item => new { item.CurrentAccountId, item.TransactionDate, item.Id });
    }
}

public sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    // Burada alış faturası başlığının toplamlarını, yaşam döngüsünü ve tedarikçi numara tekilliğini tanımlıyorum.
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("AccountingPurchaseInvoices");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CurrentAccountNameSnapshot).HasMaxLength(CurrentAccount.MaximumNameLength).IsRequired();
        builder.Property(item => item.TaxNumberSnapshot).HasMaxLength(CurrentAccount.MaximumIdentityNumberLength);
        builder.Property(item => item.TaxOfficeSnapshot).HasMaxLength(CurrentAccount.MaximumTaxOfficeLength);
        builder.Property(item => item.PhoneNumberSnapshot).HasMaxLength(CurrentAccount.MaximumPhoneLength);
        builder.Property(item => item.EmailSnapshot).HasMaxLength(CurrentAccount.MaximumEmailLength);
        builder.Property(item => item.AddressSnapshot).HasMaxLength(PurchaseInvoice.MaximumAddressSnapshotLength);
        builder.Property(item => item.InvoiceNumber).HasMaxLength(PurchaseInvoice.MaximumInvoiceNumberLength).IsRequired();
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(item => item.ExchangeRate).HasPrecision(18, 6);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(PurchaseInvoice.MaximumDescriptionLength);
        builder.Property(item => item.CancellationReason).HasMaxLength(PurchaseInvoice.MaximumDescriptionLength);
        builder.Property(item => item.InvoiceDiscountType).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.InvoiceDiscountTaxBasis).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.InvoiceDiscountValue).HasPrecision(18, 4);
        ConfigureMoney(builder);
        builder.HasOne(item => item.CurrentAccount)
            .WithMany()
            .HasForeignKey(item => item.CurrentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Lines)
            .WithOne(item => item.PurchaseInvoice)
            .HasForeignKey(item => item.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.CurrentAccountId, item.InvoiceNumber }).IsUnique();
        builder.HasIndex(item => new { item.Status, item.InvoiceDate, item.Id });
    }

    // Burada bütün fatura header para alanlarını ortak iki ondalık hassasiyete bağlıyorum.
    private static void ConfigureMoney(EntityTypeBuilder<PurchaseInvoice> builder)
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
        builder.Property(item => item.VatTotal).HasPrecision(18, 2);
        builder.Property(item => item.GrandTotalIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalAllocatedExpenseExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalAllocatedExpenseIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalFinalCostExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalFinalCostIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.PaidAmount).HasPrecision(18, 2);
        builder.Property(item => item.RemainingAmount).HasPrecision(18, 2);
    }
}

public sealed class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
{
    // Burada alış faturası satır snapshot, miktar, hesap ve allocation ilişkilerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
    {
        builder.ToTable("AccountingPurchaseInvoiceLines", table =>
        {
            table.HasCheckConstraint("CK_AccountingPurchaseInvoiceLines_Quantity", "[PurchaseQuantity] > 0 AND [UnitsPerPurchaseUnit] > 0 AND [StockQuantity] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductNameSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(item => item.VariantNameSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(item => item.SkuSnapshot).HasMaxLength(100).IsRequired();
        builder.Property(item => item.BarcodeSnapshot).HasMaxLength(100);
        builder.Property(item => item.UnitOfMeasure).HasMaxLength(50).IsRequired();
        builder.Property(item => item.PurchaseQuantity).HasPrecision(18, 4);
        builder.Property(item => item.UnitsPerPurchaseUnit).HasPrecision(18, 4);
        builder.Property(item => item.EnteredUnitPrice).HasPrecision(18, 4);
        builder.Property(item => item.UnitPriceExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.UnitPriceIncludingVat).HasPrecision(18, 4);
        builder.Property(item => item.VatRate).HasPrecision(9, 4);
        builder.Property(item => item.PriceEntryMode).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(item => item.LineDiscountType).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.LineDiscountTaxBasis).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.LineDiscountUnitBasis).HasConversion<string>().HasMaxLength(30);
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
        builder.HasMany(item => item.Allocations)
            .WithOne(item => item.PurchaseInvoiceLine)
            .HasForeignKey(item => item.PurchaseInvoiceLineId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.PurchaseInvoiceId, item.LineNumber }).IsUnique();
        builder.HasIndex(item => item.ProductVariantId);
    }

    // Burada satır para ve maliyet alanlarının veritabanı hassasiyetlerini tanımlıyorum.
    private static void ConfigureMoney(EntityTypeBuilder<PurchaseInvoiceLine> builder)
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
        builder.Property(item => item.AllocatedExpenseExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.AllocatedExpenseIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.FinalTotalCostExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.FinalTotalCostIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.FinalUnitCostExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.FinalUnitCostIncludingVat).HasPrecision(18, 4);
    }
}

public sealed class PurchaseInvoiceStockAllocationConfiguration : IEntityTypeConfiguration<PurchaseInvoiceStockAllocation>
{
    // Burada fatura satırı ile mevcut stok hareketi arasındaki kısmi allocation kaydını tanımlıyorum.
    public void Configure(EntityTypeBuilder<PurchaseInvoiceStockAllocation> builder)
    {
        builder.ToTable("AccountingPurchaseInvoiceStockAllocations", table =>
        {
            table.HasCheckConstraint("CK_AccountingPurchaseAllocations_Quantity", "[AllocatedQuantity] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasOne<StockMovement>()
            .WithMany()
            .HasForeignKey(item => item.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PurchaseInvoiceLineId, item.StockMovementId }).IsUnique();
        builder.HasIndex(item => item.StockMovementId);
    }
}

public sealed class InventoryCostLayerConfiguration : IEntityTypeConfiguration<InventoryCostLayer>
{
    // Burada allocation kaynaklı maliyet katmanını stok sisteminden bağımsız biçimde tanımlıyorum.
    public void Configure(EntityTypeBuilder<InventoryCostLayer> builder)
    {
        builder.ToTable("AccountingInventoryCostLayers", table =>
        {
            table.HasCheckConstraint("CK_AccountingCostLayers_Quantity", "[OriginalQuantity] > 0 AND [RemainingQuantity] >= 0 AND [RemainingQuantity] <= [OriginalQuantity]");
            table.HasCheckConstraint(
                "CK_AccountingCostLayers_Source",
                "([SourceType] = 'PurchaseInvoiceAllocation' AND " +
                "[PurchaseInvoiceLineId] IS NOT NULL AND " +
                "[PurchaseInvoiceStockAllocationId] IS NOT NULL) OR " +
                "([SourceType] = 'OpeningBalance' AND " +
                "[PurchaseInvoiceLineId] IS NULL AND " +
                "[PurchaseInvoiceStockAllocationId] IS NULL)");
            table.HasCheckConstraint(
                "CK_AccountingCostLayers_Cost_NonNegative",
                "[UnitCostExcludingVat] >= 0 AND [UnitCostIncludingVat] >= 0 AND " +
                "[TotalCostExcludingVat] >= 0 AND [TotalCostIncludingVat] >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SourceType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.UnitCostExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.UnitCostIncludingVat).HasPrecision(18, 4);
        builder.Property(item => item.TotalCostExcludingVat).HasPrecision(18, 2);
        builder.Property(item => item.TotalCostIncludingVat).HasPrecision(18, 2);
        builder.Property(item => item.ConcurrencyToken).IsConcurrencyToken();
        builder.HasOne(item => item.PurchaseInvoiceLine)
            .WithMany()
            .HasForeignKey(item => item.PurchaseInvoiceLineId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PurchaseInvoiceStockAllocation)
            .WithOne()
            .HasForeignKey<InventoryCostLayer>(item => item.PurchaseInvoiceStockAllocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>()
            .WithMany()
            .HasForeignKey(item => item.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.PurchaseInvoiceStockAllocationId)
            .IsUnique()
            .HasFilter("[PurchaseInvoiceStockAllocationId] IS NOT NULL");
        builder.HasIndex(item => item.StockMovementId)
            .IsUnique()
            .HasFilter("[SourceType] = 'OpeningBalance'");
        builder.HasIndex(item => new { item.ProductVariantId, item.CostDate, item.CreatedAt, item.Id });
        builder.Navigation(item => item.Consumptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductVariantCostHistoryConfiguration : IEntityTypeConfiguration<ProductVariantCostHistory>
{
    // Burada varyant maliyet geçmişinin tek aktif kayıt ve tarih sırası kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<ProductVariantCostHistory> builder)
    {
        builder.ToTable("AccountingProductVariantCostHistory", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingProductVariantCostHistory_SourceType",
                "[SourceType] IN ('PurchaseInvoice', 'OpeningBalance')");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SourceType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(item => item.PreviousCostExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.NewCostExcludingVat).HasPrecision(18, 4);
        builder.Property(item => item.PreviousCostIncludingVat).HasPrecision(18, 4);
        builder.Property(item => item.NewCostIncludingVat).HasPrecision(18, 4);
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.ProductVariantId)
            .IsUnique()
            .HasFilter("[ValidTo] IS NULL");
        builder.HasIndex(item => new
        {
            item.ProductVariantId,
            item.ValidFrom,
            item.CreatedAt,
            item.Id
        });
        builder.HasIndex(item => new { item.SourceType, item.SourceId });
    }
}
