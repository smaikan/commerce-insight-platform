using System.Text.Json.Serialization;
using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Accounting.PurchaseInvoices;

// Burada yeni alış faturası satırının katalog kimliği ile zorunlu ticari girdilerini ve varsayılan sıfır fiyatını taşıyorum.
[method: JsonConstructor]
public sealed record PurchaseInvoiceLineInput(
    int LineNumber,
    Guid ProductVariantId,
    decimal PurchaseQuantity,
    string UnitOfMeasure,
    decimal UnitsPerPurchaseUnit,
    PriceEntryMode PriceEntryMode,
    decimal VatRate,
    decimal EnteredUnitPrice = 0m,
    DiscountType? LineDiscountType = null,
    decimal? LineDiscountValue = null,
    DiscountTaxBasis? LineDiscountTaxBasis = null,
    DiscountUnitBasis? LineDiscountUnitBasis = null,
    bool IsInvoiceDiscountEligible = true)
{
    // Burada eski sunucu içi çağrı sırasını korurken dış sözleşmede fiyatın atlanabilmesini sağlıyorum.
    public PurchaseInvoiceLineInput(
        int lineNumber,
        Guid productVariantId,
        decimal purchaseQuantity,
        string unitOfMeasure,
        decimal unitsPerPurchaseUnit,
        decimal enteredUnitPrice,
        PriceEntryMode priceEntryMode,
        decimal vatRate,
        DiscountType? lineDiscountType = null,
        decimal? lineDiscountValue = null,
        DiscountTaxBasis? lineDiscountTaxBasis = null,
        DiscountUnitBasis? lineDiscountUnitBasis = null,
        bool isInvoiceDiscountEligible = true)
        : this(
            lineNumber,
            productVariantId,
            purchaseQuantity,
            unitOfMeasure,
            unitsPerPurchaseUnit,
            priceEntryMode,
            vatRate,
            enteredUnitPrice,
            lineDiscountType,
            lineDiscountValue,
            lineDiscountTaxBasis,
            lineDiscountUnitBasis,
            isInvoiceDiscountEligible)
    {
    }
}

// Burada mevcut satırın ilk ürün snapshot'ına dokunmadan değiştirilebilen ticari alanlarını taşıyorum.
public sealed record PurchaseInvoiceLineCommercialUpdateInput(
    decimal PurchaseQuantity,
    string UnitOfMeasure,
    decimal UnitsPerPurchaseUnit,
    PriceEntryMode PriceEntryMode,
    decimal VatRate,
    decimal EnteredUnitPrice = 0m,
    DiscountType? LineDiscountType = null,
    decimal? LineDiscountValue = null,
    DiscountTaxBasis? LineDiscountTaxBasis = null,
    DiscountUnitBasis? LineDiscountUnitBasis = null,
    bool IsInvoiceDiscountEligible = true);

// Burada alış faturası başlığında TRY ve birim kur varsayılanlarını açık sözleşme olarak taşıyorum.
public sealed record PurchaseInvoiceHeaderInput(
    Guid CurrentAccountId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate = null,
    string CurrencyCode = "TRY",
    decimal ExchangeRate = 1m,
    string? Description = null,
    DiscountType? InvoiceDiscountType = null,
    decimal? InvoiceDiscountValue = null,
    DiscountTaxBasis? InvoiceDiscountTaxBasis = null);

// Burada alış faturası satırının ilk snapshot, hesap, maliyet ve allocation sonuçlarını taşıyorum.
public sealed record PurchaseInvoiceLineDto(
    Guid Id,
    int LineNumber,
    string ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string? Barcode,
    decimal PurchaseQuantity,
    string UnitOfMeasure,
    decimal UnitsPerPurchaseUnit,
    int StockQuantity,
    decimal EnteredUnitPrice,
    PriceEntryMode PriceEntryMode,
    decimal UnitPriceExcludingVat,
    decimal UnitPriceIncludingVat,
    decimal VatRate,
    decimal GrossAmountExcludingVat,
    decimal GrossAmountIncludingVat,
    decimal TotalDiscountAmountExcludingVat,
    decimal TotalDiscountAmountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal VatAmount,
    decimal TotalAmountIncludingVat,
    decimal FinalUnitCostExcludingVat,
    decimal FinalUnitCostIncludingVat,
    IReadOnlyList<PurchaseInvoiceAllocationDto> Allocations);

// Burada fatura satırı ile mevcut stok hareketi arasındaki onaylı miktar tahsisini taşıyorum.
public sealed record PurchaseInvoiceAllocationDto(
    Guid Id,
    Guid StockMovementId,
    int AllocatedQuantity);

// Burada alış faturasının başlık, toplam, snapshot ve satır detaylarını birlikte taşıyorum.
public sealed record PurchaseInvoiceDto(
    Guid Id,
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
    decimal SubtotalExcludingVat,
    decimal SubtotalIncludingVat,
    decimal LineDiscountTotalExcludingVat,
    decimal LineDiscountTotalIncludingVat,
    decimal InvoiceDiscountTotalExcludingVat,
    decimal InvoiceDiscountTotalIncludingVat,
    decimal TotalDiscountExcludingVat,
    decimal TotalDiscountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal VatTotal,
    decimal GrandTotalIncludingVat,
    decimal TotalFinalCostExcludingVat,
    decimal TotalFinalCostIncludingVat,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PostedAt,
    long? CancelledBy,
    DateTime? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<PurchaseInvoiceLineDto> Lines);

// Burada alış faturası listesi için gerekli sınırlı özet alanları taşıyorum.
public sealed record PurchaseInvoiceSummaryDto(
    Guid Id,
    Guid CurrentAccountId,
    string CurrentAccountName,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string CurrencyCode,
    InvoiceStatus Status,
    decimal GrandTotalIncludingVat);

// Burada supplier veya müşteri cari hesabının istemciden gelen ana veri alanlarını taşıyorum.
public sealed record CurrentAccountInput(
    string Code,
    CurrentAccountType Type,
    string Name,
    string? TradeName = null,
    string? NationalIdentityNumber = null,
    string? TaxNumber = null,
    string? TaxOffice = null,
    string? PhoneNumber = null,
    string? Email = null,
    string? Country = null,
    string? City = null,
    string? District = null,
    string? Neighborhood = null,
    string? AddressLine = null,
    string? PostalCode = null,
    string? UserId = null);

// Burada cari hesabın dışarıya açılan kimlik, iletişim, adres ve rol alanlarını taşıyorum.
public sealed record CurrentAccountDto(
    Guid Id,
    string Code,
    CurrentAccountType Type,
    string Name,
    string? TradeName,
    string? NationalIdentityNumber,
    string? TaxNumber,
    string? TaxOffice,
    string? PhoneNumber,
    string? Email,
    string? Country,
    string? City,
    string? District,
    string? Neighborhood,
    string? AddressLine,
    string? PostalCode,
    bool IsActive,
    string? UserId);

// Burada maliyet tahsisine uygun mevcut stok hareketinin kalan miktarlarını taşıyorum.
public sealed record AvailableStockMovementDto(
    Guid Id,
    Guid ProductVariantId,
    int Quantity,
    int AllocatedQuantity,
    int AvailableQuantity,
    DateTime CreatedAt);

// Burada tek stok hareketine yapılacak alış faturası miktar tahsisini taşıyorum.
public sealed record PurchaseInvoiceAllocationInput(Guid StockMovementId, int Quantity);

// Burada katalogdan güvenilir şekilde okunan ürün ve varyant snapshot alanlarını taşıyorum.
public sealed record ProductVariantSnapshot(
    long ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string? Barcode,
    int CurrentStock);

public interface IPurchaseInvoiceRepository
{
    // Burada alış faturasını kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);
    // Burada alış faturasını detay okuması için getirme sözleşmesini tanımlıyorum.
    Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada alış faturasını değişiklik veya posting için takipli getirme sözleşmesini tanımlıyorum.
    Task<PurchaseInvoice?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada alış faturalarını sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<PurchaseInvoice>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    // Burada supplier fatura numarası tekilliğini kontrol etme sözleşmesini tanımlıyorum.
    Task<bool> InvoiceNumberExistsAsync(Guid currentAccountId, string invoiceNumber, Guid? excludedId = null, CancellationToken cancellationToken = default);
    // Burada takipli taslak faturaya eklenen yeni satırı kesin olarak Added durumuna alma sözleşmesini tanımlıyorum.
    void AddLine(PurchaseInvoiceLine line);
    // Burada taslak fatura satırını silme takibine alma sözleşmesini tanımlıyorum.
    void RemoveLine(PurchaseInvoiceLine line);
    // Burada yeni allocation kaydını ekleme takibine alma sözleşmesini tanımlıyorum.
    void AddAllocation(PurchaseInvoiceStockAllocation allocation);
}

public interface IAccountingProductSnapshotReader
{
    // Burada mevcut varyant ve ürün snapshot'ını okuma sözleşmesini tanımlıyorum.
    Task<ProductVariantSnapshot?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default);
}

public interface IAccountingStockMovementReader
{
    // Burada varyantın kullanılabilir Purchase hareketlerini listeleme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<AvailableStockMovementDto>> GetEligibleAsync(Guid productVariantId, CancellationToken cancellationToken = default);
    // Burada seçili uygun Purchase hareketlerini kimlikleriyle okuma sözleşmesini tanımlıyorum.
    Task<IReadOnlyDictionary<Guid, StockMovement>> GetEligibleByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    // Burada hareketin tahsis edilmiş toplam miktarını okuma sözleşmesini tanımlıyorum.
    Task<int> GetAllocatedQuantityAsync(Guid stockMovementId, Guid? excludedLineId = null, CancellationToken cancellationToken = default);
}

public interface IInventoryCostRepository
{
    // Burada yeni maliyet katmanını kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddLayerAsync(InventoryCostLayer layer, CancellationToken cancellationToken = default);
    // Burada allocation için maliyet katmanı tekilliğini kontrol etme sözleşmesini tanımlıyorum.
    Task<bool> LayerExistsForAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
    // Burada varyantın etkin maliyet geçmişini takipli getirme sözleşmesini tanımlıyorum.
    Task<ProductVariantCostHistory?> GetActiveHistoryForUpdateAsync(Guid productVariantId, CancellationToken cancellationToken = default);
    // Burada yeni maliyet geçmişini kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddHistoryAsync(ProductVariantCostHistory history, CancellationToken cancellationToken = default);
}

public interface ICurrentAccountRepository
{
    // Burada yeni cari hesabı kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(CurrentAccount account, CancellationToken cancellationToken = default);
    // Burada cari hesabı detay okuması için getirme sözleşmesini tanımlıyorum.
    Task<CurrentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada cari hesabı güncelleme veya posting için hareketleriyle takipli getirme sözleşmesini tanımlıyorum.
    Task<CurrentAccount?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada cari hesapları sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<CurrentAccount>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    // Burada cari hesap kodunun tekilliğini kontrol etme sözleşmesini tanımlıyorum.
    Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default);
    // Burada yeni cari hareketi EF tarafından kesin olarak Added durumunda izleme sözleşmesini tanımlıyorum.
    void AddTransaction(CurrentAccountTransaction transaction);
}
