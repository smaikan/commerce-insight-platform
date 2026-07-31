using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Accounting.PurchaseInvoices;

// Burada yeni cari hesap oluşturma isteğini taşıyorum.
public sealed record CreateCurrentAccountCommand(CurrentAccountInput Account) : IRequest<CurrentAccountDto>;
// Burada mevcut cari hesabın ana veri ve aktiflik güncellemesini taşıyorum.
public sealed record UpdateCurrentAccountCommand(Guid Id, CurrentAccountInput Account, bool IsActive) : IRequest<CurrentAccountDto>;
// Burada tek cari hesabı kimliğiyle getirme sorgusunu taşıyorum.
public sealed record GetCurrentAccountByIdQuery(Guid Id) : IRequest<CurrentAccountDto>;
// Burada cari hesapları güvenli sayfa sınırlarıyla listeleme sorgusunu taşıyorum.
public sealed record GetCurrentAccountsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<CurrentAccountDto>>;

// Burada başlık ve yeni satırlarla taslak alış faturası oluşturma isteğini taşıyorum.
public sealed record CreatePurchaseInvoiceCommand(
    PurchaseInvoiceHeaderInput Header,
    IReadOnlyList<PurchaseInvoiceLineInput> Lines) : IRequest<PurchaseInvoiceDto>;

// Burada taslak alış faturasının başlık ve bütün satırlarını güncelleme isteğini taşıyorum.
public sealed record UpdatePurchaseInvoiceCommand(
    Guid Id,
    PurchaseInvoiceHeaderInput Header,
    IReadOnlyList<PurchaseInvoiceLineInput> Lines) : IRequest<PurchaseInvoiceDto>;

// Burada taslak alış faturasına katalogdan snapshot alınacak yeni satır ekleme isteğini taşıyorum.
public sealed record AddPurchaseInvoiceLineCommand(
    Guid InvoiceId,
    PurchaseInvoiceLineInput Line) : IRequest<PurchaseInvoiceDto>;

// Burada mevcut alış faturası satırının kimlik snapshot'ına dokunmayan ticari güncelleme isteğini taşıyorum.
public sealed record UpdatePurchaseInvoiceLineCommand(
    Guid InvoiceId,
    Guid LineId,
    PurchaseInvoiceLineCommercialUpdateInput Line) : IRequest<PurchaseInvoiceDto>;

// Burada taslak alış faturasından seçili satırı kaldırma isteğini taşıyorum.
public sealed record RemovePurchaseInvoiceLineCommand(
    Guid InvoiceId,
    Guid LineId) : IRequest<PurchaseInvoiceDto>;

// Burada taslak satıra bağlanacak onaylı stok hareketi tahsislerini taşıyorum.
public sealed record SetPurchaseInvoiceAllocationsCommand(
    Guid InvoiceId,
    Guid LineId,
    IReadOnlyList<PurchaseInvoiceAllocationInput> Allocations) : IRequest<PurchaseInvoiceDto>;

// Burada alış faturasını atomik biçimde post etme isteğini taşıyorum.
public sealed record PostPurchaseInvoiceCommand(Guid Id) : IRequest<PurchaseInvoiceDto>;
// Burada alış faturası detayını kimliğiyle getirme sorgusunu taşıyorum.
public sealed record GetPurchaseInvoiceByIdQuery(Guid Id) : IRequest<PurchaseInvoiceDto>;
// Burada alış faturalarını güvenli sayfa sınırlarıyla listeleme sorgusunu taşıyorum.
public sealed record GetPurchaseInvoicesQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<PurchaseInvoiceSummaryDto>>;
// Burada varyant için tahsise uygun stok hareketlerini getirme sorgusunu taşıyorum.
public sealed record GetAvailablePurchaseStockMovementsQuery(Guid ProductVariantId) : IRequest<IReadOnlyList<AvailableStockMovementDto>>;
