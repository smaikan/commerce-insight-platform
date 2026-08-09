using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.ImportOrders;

public sealed record ImportOrderCommand(ImportedOrderInput Order) : IRequest<OrderImportResultDto>;

public sealed record BulkImportOrdersCommand(IReadOnlyList<ImportedOrderInput> Orders)
    : IRequest<IReadOnlyList<OrderImportResultDto>>;

public sealed record ImportedOrderInput(
    string OrderNumber,
    long UserId,
    decimal SubTotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    OrderStatus Status,
    IReadOnlyList<ImportedOrderItemInput> Items,
    DateTime? CreatedAtUtc = null,
    string? CouponCode = null,
    Guid? ShippingMethodId = null,
    string? ShippingMethodName = null,
    PaymentProvider? PaymentProvider = null,
    string? PaymentTransactionId = null,
    bool ApplyInventoryAndMetrics = false);

public sealed record ImportedOrderItemInput(
    long ProductId,
    Guid ProductVariantId,
    string ProductTitle,
    string VariantSku,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountTotal = 0m,
    decimal TaxRatePercentage = 0m,
    decimal TaxTotal = 0m);
