using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.ImportOrders;

public sealed class ImportOrderCommandHandler : IRequestHandler<ImportOrderCommand, OrderImportResultDto>
{
    private readonly ImportedOrderProcessor _processor;

    public ImportOrderCommandHandler(ImportedOrderProcessor processor)
    {
        _processor = processor;
    }

    public Task<OrderImportResultDto> Handle(ImportOrderCommand request, CancellationToken cancellationToken) =>
        _processor.ImportAsync(request.Order, cancellationToken);
}

public sealed class BulkImportOrdersCommandHandler
    : IRequestHandler<BulkImportOrdersCommand, IReadOnlyList<OrderImportResultDto>>
{
    private readonly ImportedOrderProcessor _processor;
    private readonly IUnitOfWork _unitOfWork;

    public BulkImportOrdersCommandHandler(ImportedOrderProcessor processor, IUnitOfWork unitOfWork)
    {
        _processor = processor;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<OrderImportResultDto>> Handle(
        BulkImportOrdersCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Orders.Count is < 1 or > ImportedOrderProcessor.MaximumBatchSize)
        {
            throw new ConflictException($"Order import batch size must be between 1 and {ImportedOrderProcessor.MaximumBatchSize}.");
        }

        var normalizedNumbers = request.Orders.Select(order => order.OrderNumber?.Trim()).ToList();
        if (normalizedNumbers.Any(string.IsNullOrWhiteSpace) ||
            normalizedNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedNumbers.Count)
        {
            throw new ConflictException("Imported order numbers must be non-empty and unique within a batch.");
        }

        return _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCancellationToken =>
        {
            var results = new List<OrderImportResultDto>(request.Orders.Count);
            foreach (var order in request.Orders)
            {
                results.Add(await _processor.ImportCoreAsync(order, transactionCancellationToken));
            }

            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
            return (IReadOnlyList<OrderImportResultDto>)results;
        }, cancellationToken);
    }
}

// Burada tekli ve toplu importun aynı idempotency, stok ve yaşam döngüsü kurallarını kullanmasını sağlıyorum.
public sealed class ImportedOrderProcessor
{
    public const int MaximumBatchSize = 500;

    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IOrderMetricsRecorder _metricsRecorder;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthoritativeSalesMetricService _salesMetrics;

    // Burada import işlemcisini sipariş, katalog, metrik, saat ve transaction bağımlılıklarıyla hazırlıyorum.
    public ImportedOrderProcessor(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IProductVariantRepository variantRepository,
        IProductRepository productRepository,
        IShippingMethodRepository shippingMethodRepository,
        IOrderMetricsRecorder metricsRecorder,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IAuthoritativeSalesMetricService salesMetrics)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _metricsRecorder = metricsRecorder;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _salesMetrics = salesMetrics;
    }

    public Task<OrderImportResultDto> ImportAsync(ImportedOrderInput input, CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCancellationToken =>
        {
            var result = await ImportCoreAsync(input, transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
            return result;
        }, cancellationToken);

    // Burada tek bir harici siparişi idempotent biçimde doğrulayıp lifecycle ve metrik etkileriyle hazırlıyorum.
    public async Task<OrderImportResultDto> ImportCoreAsync(ImportedOrderInput input, CancellationToken cancellationToken)
    {
        var orderNumber = NormalizeOrderNumber(input.OrderNumber);
        var existing = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);
        if (existing is not null)
        {
            return new OrderImportResultDto(existing.ToDto(), false);
        }

        if (input.UserId <= 0 || await _userRepository.GetByIdAsync(input.UserId, cancellationToken) is null)
        {
            throw new NotFoundException("Order owner was not found.");
        }

        ValidateInput(input);
        var shipping = await ResolveShippingMethodAsync(input, cancellationToken);
        var variants = await ResolveVariantsAsync(input.Items, cancellationToken);
        var order = new Order(
            input.UserId,
            orderNumber,
            input.SubTotal,
            input.DiscountTotal,
            input.ShippingTotal,
            input.TaxTotal,
            input.GrandTotal,
            couponCode: input.CouponCode,
            shippingMethodId: shipping?.Id,
            shippingMethodName: shipping?.Name);

        if (input.CreatedAtUtc.HasValue)
        {
            order.SetImportedCreatedAt(input.CreatedAtUtc.Value);
        }

        foreach (var item in input.Items)
        {
            order.AddItem(
                item.ProductId,
                item.ProductVariantId,
                item.ProductTitle,
                item.VariantSku,
                item.UnitPrice,
                item.Quantity,
                item.DiscountTotal,
                item.TaxRatePercentage,
                item.TaxTotal);
        }

        order.EnsureItemsMatchSubTotal();
        ApplyStatus(order, input);
        await _orderRepository.AddAsync(order, cancellationToken);

        if (input.ApplyInventoryAndMetrics)
        {
            await ApplyFulfillmentEffectsAsync(order, input, variants, cancellationToken);
        }

        if (RequiresPaidPayment(input.Status))
        {
            await _salesMetrics.RecordPaidOrderAsync(order, cancellationToken);
            if (input.Status == OrderStatus.Refunded)
            {
                await _salesMetrics.ReverseCancelledOrderAsync(order, cancellationToken);
            }
        }

        return new OrderImportResultDto(order.ToDto(), true);
    }

    private static void ValidateInput(ImportedOrderInput input)
    {
        if (!Enum.IsDefined(input.Status) || input.Items.Count == 0 || input.Items.Count > Order.MaximumItemCount)
        {
            throw new ConflictException("Imported order status or item count is invalid.");
        }

        if (input.CreatedAtUtc.HasValue && input.CreatedAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ConflictException("Imported order creation time must be UTC.");
        }

        if (input.Items.Select(item => item.ProductVariantId).Distinct().Count() != input.Items.Count ||
            input.Items.Any(item => item.ProductId <= 0 || item.ProductVariantId == Guid.Empty))
        {
            throw new ConflictException("Imported order items must contain unique valid product variants.");
        }

        if (input.Status is OrderStatus.ReturnRequested or OrderStatus.ReturnApproved)
        {
            throw new ConflictException("Return workflow statuses cannot be imported without their return records.");
        }

        if (input.ApplyInventoryAndMetrics && !ShouldApplyFulfillmentEffects(input.Status))
        {
            throw new ConflictException("Inventory and metrics can only be applied to paid, preparing, shipped, or delivered imports.");
        }

        if (RequiresPaidPayment(input.Status) && input.GrandTotal > 0m &&
            (!input.PaymentProvider.HasValue || string.IsNullOrWhiteSpace(input.PaymentTransactionId)))
        {
            throw new ConflictException("A payment provider and transaction id are required for a paid imported order.");
        }
    }

    private async Task<ShippingMethod?> ResolveShippingMethodAsync(
        ImportedOrderInput input,
        CancellationToken cancellationToken)
    {
        if (!input.ShippingMethodId.HasValue && string.IsNullOrWhiteSpace(input.ShippingMethodName))
        {
            if (input.ShippingTotal > 0m)
            {
                throw new ConflictException("A shipping method is required when an imported shipping fee is charged.");
            }

            return null;
        }

        if (!input.ShippingMethodId.HasValue || string.IsNullOrWhiteSpace(input.ShippingMethodName))
        {
            throw new ConflictException("Imported shipping method id and name must be provided together.");
        }

        var shippingMethod = await _shippingMethodRepository.GetByIdAsync(
            input.ShippingMethodId.Value,
            cancellationToken)
            ?? throw new NotFoundException("Imported shipping method was not found.");
        if (!string.Equals(shippingMethod.Name, input.ShippingMethodName.Trim(), StringComparison.Ordinal))
        {
            throw new ConflictException("Imported shipping method name does not match its id.");
        }

        return shippingMethod;
    }

    private async Task<IReadOnlyDictionary<Guid, ProductVariant>> ResolveVariantsAsync(
        IReadOnlyList<ImportedOrderItemInput> items,
        CancellationToken cancellationToken)
    {
        var variants = await _variantRepository.GetByIdsForUpdateAsync(
            items.Select(item => item.ProductVariantId),
            cancellationToken);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        if (variantsById.Count != items.Count || items.Any(item =>
                !variantsById.TryGetValue(item.ProductVariantId, out var variant) ||
                variant.ProductId != item.ProductId))
        {
            throw new ConflictException("An imported order item does not match an existing product variant.");
        }

        return variantsById;
    }

    private void ApplyStatus(Order order, ImportedOrderInput input)
    {
        var occurredAtUtc = input.CreatedAtUtc ?? _clock.UtcNow;
        if (input.Status == OrderStatus.Pending)
        {
            return;
        }

        order.ChangeStatus(OrderStatus.Confirmed, occurredAtUtc);
        if (input.Status == OrderStatus.Confirmed)
        {
            return;
        }

        if (input.Status == OrderStatus.Cancelled)
        {
            order.ChangeStatus(OrderStatus.Cancelled, occurredAtUtc);
            return;
        }

        Payment? payment = null;
        if (order.GrandTotal > 0m)
        {
            payment = new Payment(order.Id, input.PaymentProvider!.Value, order.GrandTotal);
            order.AddPayment(payment);
            payment.MarkAsPaid(input.PaymentTransactionId!);
        }

        order.ChangeStatus(OrderStatus.Paid, occurredAtUtc);
        if (input.Status == OrderStatus.Paid)
        {
            return;
        }

        if (input.Status == OrderStatus.Refunded)
        {
            order.ChangeStatus(OrderStatus.Refunded, occurredAtUtc);
            payment?.MarkAsRefunded();
            return;
        }

        order.ChangeStatus(OrderStatus.Preparing, occurredAtUtc);
        if (input.Status == OrderStatus.Preparing)
        {
            return;
        }

        order.ChangeStatus(OrderStatus.Shipped, occurredAtUtc);
        if (input.Status == OrderStatus.Shipped)
        {
            return;
        }

        order.ChangeStatus(OrderStatus.Delivered, occurredAtUtc);
    }

    private async Task ApplyFulfillmentEffectsAsync(
        Order order,
        ImportedOrderInput input,
        IReadOnlyDictionary<Guid, ProductVariant> variantsById,
        CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetByIdsForUpdateAsync(
            input.Items.Select(item => item.ProductId),
            cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);
        if (productsById.Count != input.Items.Select(item => item.ProductId).Distinct().Count())
        {
            throw new ConflictException("An imported order item references a missing product.");
        }

        var metricLines = new List<PurchaseMetricLine>(input.Items.Count);
        foreach (var item in input.Items)
        {
            var variant = variantsById[item.ProductVariantId];
            if (item.Quantity > variant.Stock)
            {
                throw new ConflictException("Imported order quantity exceeds available stock.");
            }

            variant.ApplyStockMovement(-item.Quantity, StockMovementType.Sale, "Imported order.", order.Id);
            metricLines.Add(new PurchaseMetricLine(productsById[item.ProductId], variant, item.Quantity));
        }

        await _metricsRecorder.RecordPurchasedQuantitiesAsync(metricLines, cancellationToken);
    }

    private static bool RequiresPaidPayment(OrderStatus status) =>
        status is OrderStatus.Paid or OrderStatus.Preparing or OrderStatus.Shipped or
            OrderStatus.Delivered or OrderStatus.Refunded;

    private static bool ShouldApplyFulfillmentEffects(OrderStatus status) =>
        status is OrderStatus.Paid or OrderStatus.Preparing or OrderStatus.Shipped or OrderStatus.Delivered;

    private static string NormalizeOrderNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ConflictException("Imported order number is required.");
        }

        return value.Trim();
    }
}
