using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Accounting.SalesOrders;

// Burada tek Accounting satış satırının istemciden gelen ham ticari girdilerini taşıyorum.
public sealed record AccountingSalesOrderLineInput(
    int LineNumber,
    Guid ProductVariantId,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitsPerSaleUnit,
    PriceEntryMode PriceEntryMode,
    decimal VatRate,
    decimal EnteredUnitPrice = 0m,
    DiscountType? LineDiscountType = null,
    decimal? LineDiscountValue = null,
    DiscountTaxBasis? LineDiscountTaxBasis = null,
    DiscountUnitBasis? LineDiscountUnitBasis = null,
    bool IsInvoiceDiscountEligible = true);

// Burada Accounting satış siparişinin açıkça girilen cari, tarih, para ve indirim başlığını taşıyorum.
public sealed record AccountingSalesOrderHeaderInput(
    Guid CurrentAccountId,
    string OrderNumber,
    DateTime OrderDate,
    DateTime? DueDate = null,
    string CurrencyCode = "TRY",
    decimal ExchangeRate = 1m,
    decimal ShippingTotal = 0m,
    string? Description = null,
    DiscountType? InvoiceDiscountType = null,
    decimal? InvoiceDiscountValue = null,
    DiscountTaxBasis? InvoiceDiscountTaxBasis = null,
    ShippingPayer ShippingPayer = ShippingPayer.None);

// Burada mevcut fatura satırının ürün kimliğine dokunmadan değiştirilebilen ticari alanları taşıyorum.
public sealed record SalesInvoiceLineUpdateInput(
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitsPerSaleUnit,
    PriceEntryMode PriceEntryMode,
    decimal VatRate,
    decimal EnteredUnitPrice = 0m,
    DiscountType? LineDiscountType = null,
    decimal? LineDiscountValue = null,
    DiscountTaxBasis? LineDiscountTaxBasis = null,
    DiscountUnitBasis? LineDiscountUnitBasis = null,
    bool IsInvoiceDiscountEligible = true);

// Burada isteğe bağlı iç satış faturasının istemci tarafından verilen belge başlığını taşıyorum.
public sealed record SalesInvoiceHeaderInput(
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string? Description);

// Burada satış item'ıyla mevcut stok hareketi arasındaki bağlantıyı dış sözleşmeye taşıyorum.
public sealed record AccountingSalesOrderStockMovementDto(
    Guid Id,
    Guid StockMovementId,
    int Quantity);

// Burada satış satırının hangi FIFO maliyet katmanlarından beslendiğini değişmez maliyetleriyle taşıyorum.
public sealed record CostLayerConsumptionDto(
    Guid Id,
    Guid InventoryCostLayerId,
    Guid StockMovementId,
    int Quantity,
    decimal UnitCostExcludingVat,
    decimal TotalCostExcludingVat,
    DateTime CreatedAt);

// Burada Accounting satış item'ının snapshot, hesap, maliyet, kâr ve stok bağlantılarını taşıyorum.
public sealed record AccountingSalesOrderItemDto(
    Guid Id,
    int LineNumber,
    string ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string? Barcode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitsPerSaleUnit,
    int StockQuantity,
    decimal EnteredUnitPrice,
    PriceEntryMode PriceEntryMode,
    decimal UnitPriceExcludingVat,
    decimal UnitPriceIncludingVat,
    decimal VatRate,
    DiscountType? LineDiscountType,
    decimal? LineDiscountValue,
    DiscountTaxBasis? LineDiscountTaxBasis,
    DiscountUnitBasis? LineDiscountUnitBasis,
    bool IsInvoiceDiscountEligible,
    decimal GrossAmountExcludingVat,
    decimal GrossAmountIncludingVat,
    decimal LineDiscountAmountExcludingVat,
    decimal LineDiscountAmountIncludingVat,
    decimal InvoiceDiscountShareExcludingVat,
    decimal InvoiceDiscountShareIncludingVat,
    decimal TotalDiscountAmountExcludingVat,
    decimal TotalDiscountAmountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal VatAmount,
    decimal TotalAmountIncludingVat,
    decimal CostOfGoodsSold,
    decimal GrossProfitExcludingVat,
    decimal GrossProfitMargin,
    IReadOnlyList<AccountingSalesOrderStockMovementDto> StockMovements,
    IReadOnlyList<CostLayerConsumptionDto> CostLayerConsumptions);

// Burada Accounting satış siparişinin bütün tarihsel header toplamları ve item detaylarını taşıyorum.
public sealed record AccountingSalesOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CurrentAccountId,
    string CurrentAccountName,
    string? TaxNumberSnapshot,
    string? TaxOfficeSnapshot,
    string? PhoneNumberSnapshot,
    string? EmailSnapshot,
    string? AddressSnapshot,
    DateTime OrderDate,
    DateTime? DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    InvoiceStatus Status,
    string? Description,
    DiscountType? InvoiceDiscountType,
    decimal? InvoiceDiscountValue,
    DiscountTaxBasis? InvoiceDiscountTaxBasis,
    decimal SubtotalExcludingVat,
    decimal SubtotalIncludingVat,
    decimal LineDiscountTotalExcludingVat,
    decimal LineDiscountTotalIncludingVat,
    decimal InvoiceDiscountTotalExcludingVat,
    decimal InvoiceDiscountTotalIncludingVat,
    decimal TotalDiscountExcludingVat,
    decimal TotalDiscountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal ShippingTotal,
    ShippingPayer ShippingPayer,
    decimal VatTotal,
    decimal GrandTotalIncludingVat,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal TotalCostOfGoodsSold,
    decimal GrossProfitExcludingVat,
    decimal GrossProfitMargin,
    Guid? SalesInvoiceId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PostedAt,
    long? CancelledBy,
    DateTime? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<AccountingSalesOrderItemDto> Items);

// Burada Accounting satış siparişi listesi için PII içermeyen özet alanları taşıyorum.
public sealed record AccountingSalesOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    Guid CurrentAccountId,
    string CurrentAccountName,
    DateTime OrderDate,
    InvoiceStatus Status,
    decimal GrandTotalIncludingVat,
    Guid? SalesInvoiceId);

// Burada iç satış faturası satırının değişmez snapshot, hesap, maliyet ve kârlılık alanlarını taşıyorum.
public sealed record SalesInvoiceLineDto(
    Guid Id,
    Guid AccountingSalesOrderItemId,
    int LineNumber,
    string ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string? Barcode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitsPerSaleUnit,
    int StockQuantity,
    decimal EnteredUnitPrice,
    PriceEntryMode PriceEntryMode,
    decimal UnitPriceExcludingVat,
    decimal UnitPriceIncludingVat,
    decimal VatRate,
    DiscountType? LineDiscountType,
    decimal? LineDiscountValue,
    DiscountTaxBasis? LineDiscountTaxBasis,
    DiscountUnitBasis? LineDiscountUnitBasis,
    bool IsInvoiceDiscountEligible,
    decimal GrossAmountExcludingVat,
    decimal GrossAmountIncludingVat,
    decimal LineDiscountAmountExcludingVat,
    decimal LineDiscountAmountIncludingVat,
    decimal InvoiceDiscountShareExcludingVat,
    decimal InvoiceDiscountShareIncludingVat,
    decimal TotalDiscountAmountExcludingVat,
    decimal TotalDiscountAmountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal VatAmount,
    decimal TotalAmountIncludingVat,
    decimal CostOfGoodsSold,
    decimal GrossProfitExcludingVat,
    decimal GrossProfitMargin,
    IReadOnlyList<CostLayerConsumptionDto> CostLayerConsumptions);

// Burada iç satış faturasının sipariş bağı, snapshot, toplam ve satır detaylarını taşıyorum.
public sealed record SalesInvoiceDto(
    Guid Id,
    Guid AccountingSalesOrderId,
    Guid CurrentAccountId,
    string CurrentAccountName,
    string? TaxNumberSnapshot,
    string? TaxOfficeSnapshot,
    string? PhoneNumberSnapshot,
    string? EmailSnapshot,
    string? AddressSnapshot,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    InvoiceStatus Status,
    string? Description,
    DiscountType? InvoiceDiscountType,
    decimal? InvoiceDiscountValue,
    DiscountTaxBasis? InvoiceDiscountTaxBasis,
    decimal SubtotalExcludingVat,
    decimal SubtotalIncludingVat,
    decimal LineDiscountTotalExcludingVat,
    decimal LineDiscountTotalIncludingVat,
    decimal InvoiceDiscountTotalExcludingVat,
    decimal InvoiceDiscountTotalIncludingVat,
    decimal TotalDiscountExcludingVat,
    decimal TotalDiscountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal ShippingTotal,
    ShippingPayer ShippingPayer,
    decimal VatTotal,
    decimal GrandTotalIncludingVat,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal TotalCostOfGoodsSold,
    decimal GrossProfitExcludingVat,
    decimal GrossProfitMargin,
    DateTime CreatedAt,
    DateTime? PostedAt,
    long? CancelledBy,
    DateTime? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<SalesInvoiceLineDto> Lines);

// Burada iç satış faturası listesi için PII içermeyen özet alanları taşıyorum.
public sealed record SalesInvoiceSummaryDto(
    Guid Id,
    Guid AccountingSalesOrderId,
    string InvoiceNumber,
    Guid CurrentAccountId,
    string CurrentAccountName,
    DateTime InvoiceDate,
    InvoiceStatus Status,
    decimal GrandTotalIncludingVat);

// Burada mevcut Product ve ProductVariant tablolarından okunan güvenilir satış snapshot'ını taşıyorum.
public sealed record AccountingSalesProductSnapshot(
    long ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string? Barcode,
    bool ProductIsActive,
    bool VariantIsActive,
    int CurrentStock);

public interface IAccountingSalesOrderRepository
{
    // Burada yeni muhasebe satış siparişini takip etmeye başlıyorum.
    Task AddAsync(AccountingSalesOrder order, CancellationToken cancellationToken = default);
    // Burada satış siparişini detay görüntüleme için takip etmeden getiriyorum.
    Task<AccountingSalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada satış siparişini draft değişikliği veya posting için takipli getiriyorum.
    Task<AccountingSalesOrder?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada istemci tekrarlarında aynı satış siparişini idempotency anahtarıyla buluyorum.
    Task<AccountingSalesOrder?> GetByIdempotencyKeyForUpdateAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    // Burada muhasebe satış siparişlerini kararlı ve sayfalı biçimde getiriyorum.
    Task<PagedResult<AccountingSalesOrder>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    // Burada satış siparişi numarasının tekilliğini kontrol ediyorum.
    Task<bool> OrderNumberExistsAsync(
        string orderNumber,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);
    // Burada takipli mevcut aggregate'a eklenen yeni draft item'ı kesin olarak Added durumuna alıyorum.
    void AddItem(AccountingSalesOrderItem item);
    // Burada aggregate'dan çıkarılan draft kalemini EF silme takibine alıyorum.
    void RemoveItem(AccountingSalesOrderItem item);
    // Burada oluşturulan stok hareketi bağlantısını kesin olarak yeni kayıt halinde izliyorum.
    void AddStockMovementLink(AccountingSalesOrderStockMovement link);
}

public interface ISalesInvoiceRepository
{
    // Burada yeni iç satış faturasını takip etmeye başlıyorum.
    Task AddAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);
    // Burada satış faturasını detaylarıyla takip etmeden getiriyorum.
    Task<SalesInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada satış faturasını posting yönlendirmesi için takipli getiriyorum.
    Task<SalesInvoice?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada satış faturalarını kararlı ve sayfalı biçimde getiriyorum.
    Task<PagedResult<SalesInvoice>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    // Burada cari hesap ve fatura numarası birleşiminin tekilliğini kontrol ediyorum.
    Task<bool> InvoiceNumberExistsAsync(
        Guid currentAccountId,
        string invoiceNumber,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);
    // Burada takipli mevcut faturaya eklenen yeni snapshot satırını kesin olarak Added durumuna alıyorum.
    void AddLine(SalesInvoiceLine line);
    // Burada snapshot yenilemesinde çıkarılan eski fatura satırını silme takibine alıyorum.
    void RemoveLine(SalesInvoiceLine line);
}

public interface IAccountingSalesCatalogReader
{
    // Burada Accounting isteğindeki varyantların güvenilir ürün snapshot'larını toplu okuyorum.
    Task<IReadOnlyDictionary<Guid, AccountingSalesProductSnapshot>> GetByVariantIdsAsync(
        IEnumerable<Guid> productVariantIds,
        CancellationToken cancellationToken = default);
}

public interface IAccountingSalesCostRepository
{
    // Burada açık maliyet katmanlarını gerçek FIFO sırasında ve takipli getiriyorum.
    Task<IReadOnlyList<InventoryCostLayer>> GetOpenLayersForUpdateAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
    // Burada yeni FIFO tüketimini kesin olarak Added durumunda izliyorum.
    void AddConsumption(CostLayerConsumption consumption);
}
