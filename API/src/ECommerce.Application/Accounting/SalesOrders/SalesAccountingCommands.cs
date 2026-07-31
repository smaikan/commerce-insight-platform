using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Accounting.SalesOrders;

// Burada doğrudan Accounting satırlarından yeni taslak satış siparişi oluşturma isteğini tanımlıyorum.
public sealed record CreateAccountingSalesOrderCommand(
    string IdempotencyKey,
    AccountingSalesOrderHeaderInput Header,
    IReadOnlyList<AccountingSalesOrderLineInput> Lines,
    bool CreateInvoice,
    SalesInvoiceHeaderInput? Invoice) : IRequest<AccountingSalesOrderDto>;

// Burada yalnız taslak Accounting satış siparişinin başlık ve satırlarını değiştirme isteğini tanımlıyorum.
public sealed record UpdateAccountingSalesOrderCommand(
    Guid Id,
    AccountingSalesOrderHeaderInput Header,
    IReadOnlyList<AccountingSalesOrderLineInput> Lines) : IRequest<AccountingSalesOrderDto>;

// Burada taslak Accounting satış siparişine tek satır ekleme isteğini tanımlıyorum.
public sealed record AddAccountingSalesOrderItemCommand(
    Guid OrderId,
    AccountingSalesOrderLineInput Line) : IRequest<AccountingSalesOrderDto>;

// Burada taslak Accounting satış siparişindeki tek satırın ürün kimliğine dokunmadan ticari alanlarını değiştirme isteğini tanımlıyorum.
public sealed record UpdateAccountingSalesOrderItemCommand(
    Guid OrderId,
    Guid ItemId,
    SalesInvoiceLineUpdateInput Line) : IRequest<AccountingSalesOrderDto>;

// Burada taslak Accounting satış siparişinden tek satır kaldırma isteğini tanımlıyorum.
public sealed record RemoveAccountingSalesOrderItemCommand(
    Guid OrderId,
    Guid ItemId) : IRequest<AccountingSalesOrderDto>;

// Burada Accounting satış siparişini stok, FIFO ve cari etkileriyle atomik post etme isteğini tanımlıyorum.
public sealed record PostAccountingSalesOrderCommand(Guid Id) : IRequest<AccountingSalesOrderDto>;

// Burada Accounting satış siparişi detayını bütün satır ve stok bağlantılarıyla istemeyi tanımlıyorum.
public sealed record GetAccountingSalesOrderByIdQuery(Guid Id) : IRequest<AccountingSalesOrderDto>;

// Burada Accounting satış siparişlerini güvenli sayfalama ile istemeyi tanımlıyorum.
public sealed record GetAccountingSalesOrdersQuery(
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<AccountingSalesOrderSummaryDto>>;

// Burada mevcut Accounting satış siparişinden daha sonra tek iç fatura üretme isteğini tanımlıyorum.
public sealed record CreateSalesInvoiceFromOrderCommand(
    Guid AccountingSalesOrderId,
    SalesInvoiceHeaderInput Header) : IRequest<SalesInvoiceDto>;

// Burada doğrudan fatura girişinin aynı işlemde tek Accounting satış siparişi üretmesini tanımlıyorum.
public sealed record CreateDirectSalesInvoiceCommand(
    string IdempotencyKey,
    AccountingSalesOrderHeaderInput OrderHeader,
    SalesInvoiceHeaderInput InvoiceHeader,
    IReadOnlyList<AccountingSalesOrderLineInput> Lines) : IRequest<SalesInvoiceDto>;

// Burada yalnız taslak iç satış faturasının belge başlığını güncelleme isteğini tanımlıyorum.
public sealed record UpdateSalesInvoiceCommand(
    Guid Id,
    SalesInvoiceHeaderInput Header,
    IReadOnlyList<AccountingSalesOrderLineInput>? Lines = null) : IRequest<SalesInvoiceDto>;

// Burada taslak fatura arayüzünden yeni varyant satırı ekleyip bağlı siparişe yansıtma isteğini tanımlıyorum.
public sealed record AddSalesInvoiceLineCommand(
    Guid InvoiceId,
    AccountingSalesOrderLineInput Line) : IRequest<SalesInvoiceDto>;

// Burada taslak fatura satırının yalnız ticari alanlarını bağlı sipariş satırıyla birlikte değiştirmeyi tanımlıyorum.
public sealed record UpdateSalesInvoiceLineCommand(
    Guid InvoiceId,
    Guid LineId,
    SalesInvoiceLineUpdateInput Line) : IRequest<SalesInvoiceDto>;

// Burada taslak fatura satırını bağlı sipariş satırıyla birlikte kaldırma isteğini tanımlıyorum.
public sealed record RemoveSalesInvoiceLineCommand(
    Guid InvoiceId,
    Guid LineId) : IRequest<SalesInvoiceDto>;

// Burada iç satış faturası posting talebini bağlı Accounting satış siparişine yönlendirmeyi tanımlıyorum.
public sealed record PostSalesInvoiceCommand(Guid Id) : IRequest<SalesInvoiceDto>;

// Burada iç satış faturası detayını snapshot satırlarıyla istemeyi tanımlıyorum.
public sealed record GetSalesInvoiceByIdQuery(Guid Id) : IRequest<SalesInvoiceDto>;

// Burada iç satış faturalarını güvenli sayfalama ile istemeyi tanımlıyorum.
public sealed record GetSalesInvoicesQuery(
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<SalesInvoiceSummaryDto>>;
