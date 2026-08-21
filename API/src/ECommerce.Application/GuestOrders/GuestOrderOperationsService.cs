using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Returns.Commands.CreateReturnRequest;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.GuestOrders;

public sealed class GuestOrderOperationsService
{
    private readonly GuestOrderAccessService _access;
    private readonly IGuestOrderRepository _guestOrders;
    private readonly IOrderRepository _orders;
    private readonly IReturnRequestRepository _returns;
    private readonly IProductVariantRepository _variants;
    private readonly IReadOnlyCollection<IPaymentGateway> _paymentGateways;
    private readonly OrderInventoryService _inventory;
    private readonly OrderCouponService _coupons;
    private readonly IOrderNotificationService _notifications;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada guest ödeme, iptal ve iade işlemlerinin mevcut domain servisleriyle ortak bağımlılıklarını hazırlıyorum.
    public GuestOrderOperationsService(
        GuestOrderAccessService access,
        IGuestOrderRepository guestOrders,
        IOrderRepository orders,
        IReturnRequestRepository returns,
        IProductVariantRepository variants,
        IEnumerable<IPaymentGateway> paymentGateways,
        OrderInventoryService inventory,
        OrderCouponService coupons,
        IOrderNotificationService notifications,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _access = access;
        _guestOrders = guestOrders;
        _orders = orders;
        _returns = returns;
        _variants = variants;
        _paymentGateways = paymentGateways.ToList();
        _inventory = inventory;
        _coupons = coupons;
        _notifications = notifications;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    // Burada guest sipariş için idempotent ödeme kaydını oluşturup sağlayıcı sonucunu güvenle uygularım.
    public async Task<PaymentDto> CreatePaymentAsync(
        string sessionToken,
        string csrfToken,
        Guid orderId,
        PaymentProvider provider,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var normalizedKey = Payment.NormalizeIdempotencyKey(idempotencyKey);
        var gateway = _paymentGateways.SingleOrDefault(candidate => candidate.Provider == provider)
            ?? throw new ConflictException("The selected payment provider is not configured.");
        PaymentDto? existing = null;
        Guid? paymentId = null;
        decimal amount = 0m;
        var created = await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var session = await _access.ValidateSessionAsync(sessionToken, csrfToken, true, token);
                var order = await _guestOrders.GetOrderForSessionAsync(session.Id, orderId, true, token)
                    ?? throw new NotFoundException("Order was not found.");
                var matching = order.Payments.SingleOrDefault(payment => payment.IdempotencyKey == normalizedKey);
                if (matching is not null)
                {
                    existing = matching.ToDto();
                    return false;
                }

                EnsurePaymentCanStart(order);

                var payment = new Payment(order.Id, provider, order.GrandTotal, normalizedKey);
                order.AddPayment(payment);
                await _orders.AddPaymentAsync(payment, token);
                paymentId = payment.Id;
                amount = payment.Amount;
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
        if (!created)
        {
            return existing ?? throw new ConflictException("Payment attempt could not be resolved.");
        }

        var result = await ChargeAsync(gateway, orderId, amount, normalizedKey, cancellationToken);
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(
            token => CompletePaymentAsync(orderId, paymentId!.Value, result, token),
            cancellationToken);
    }

    // Burada guest müşterinin yalnız ödeme öncesi siparişini mevcut stok geri alma ve kupon release akışıyla iptal ediyorum.
    public Task<OrderDto> CancelAsync(
        string sessionToken,
        string csrfToken,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var session = await _access.ValidateSessionAsync(sessionToken, csrfToken, true, token);
                var order = await _guestOrders.GetOrderForSessionAsync(session.Id, orderId, true, token)
                    ?? throw new NotFoundException("Order was not found.");
                if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Paid or OrderStatus.Preparing))
                {
                    throw new ConflictException("Only orders that have not been shipped can be cancelled by the customer.");
                }

                if (order.Payments.Any(payment => payment.Status == PaymentStatus.Pending))
                {
                    throw new ConflictException("A payment attempt requires reconciliation before cancellation.");
                }

                await _inventory.RestoreCancelledOrderStockAsync(order, token);
                await _coupons.ReleaseForCancellationAsync(order, token);
                order.ChangeStatus(OrderStatus.Cancelled, _clock.UtcNow);
                await _notifications.QueueOrderStatusChangedAsync(order, token);
                await _unitOfWork.SaveChangesAsync(token);
                return order.ToDto();
            },
            cancellationToken);
    }

    // Burada guest sipariş için teslimat sonrası iade veya değişim talebini mevcut miktar kurallarıyla oluşturuyorum.
    public Task<ReturnRequestDto> CreateReturnAsync(
        string sessionToken,
        string csrfToken,
        Guid orderId,
        ReturnType type,
        IReadOnlyList<CreateReturnItemCommand> items,
        string? customerNote,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            token => CreateReturnInTransactionAsync(
                sessionToken, csrfToken, orderId, type, items, customerNote, token),
            cancellationToken);
    }

    // Burada guest session'ın seçili siparişteki iade taleplerini sayfalı getiriyorum.
    public async Task<PagedResult<ReturnRequestSummaryDto>> GetReturnsAsync(
        string sessionToken,
        Guid orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var session = await _access.ValidateSessionAsync(sessionToken, null, false, cancellationToken);
        if (await _guestOrders.GetOrderForSessionAsync(session.Id, orderId, false, cancellationToken) is null)
        {
            throw new NotFoundException("Order was not found.");
        }

        var requests = await _guestOrders.GetReturnsForSessionOrderAsync(
            session.Id, orderId, pageNumber, pageSize, cancellationToken);
        return requests.Map(request => request.ToSummaryDto());
    }

    // Burada guest session'ın yalnız kendi siparişine bağlı iade talebi detayını getiriyorum.
    public async Task<ReturnRequestDto> GetReturnAsync(
        string sessionToken,
        Guid orderId,
        Guid returnId,
        CancellationToken cancellationToken)
    {
        var session = await _access.ValidateSessionAsync(sessionToken, null, false, cancellationToken);
        var request = await _guestOrders.GetReturnForSessionAsync(
            session.Id, orderId, returnId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        return request.ToDto();
    }

    // Burada ödeme başlatma durumunu üyeli checkout ile aynı kurallarla doğruluyorum.
    private static void EnsurePaymentCanStart(Order order)
    {
        if (order.GrandTotal == 0m)
        {
            throw new ConflictException("This order does not require a payment.");
        }

        if (order.Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
        {
            throw new ConflictException("This order cannot accept another payment attempt.");
        }

        if (order.Payments.Any(payment => payment.Status == PaymentStatus.Pending))
        {
            throw new ConflictException("Another payment attempt is still being processed.");
        }
    }

    // Burada ödeme sağlayıcısı hatasını güvenli başarısız sonuç biçimine dönüştürüyorum.
    private static async Task<PaymentGatewayResult> ChargeAsync(
        IPaymentGateway gateway,
        Guid orderId,
        decimal amount,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            return await gateway.ChargeAsync(
                new PaymentGatewayRequest(orderId, amount, idempotencyKey), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new PaymentGatewayResult(false, null, "Payment provider communication failed.");
        }
    }

    // Burada önceden yetkilendirilmiş sağlayıcı sonucunu session claim/süre yarışından etkilenmeden bekleyen ödeme kaydına atomik uyguluyorum.
    private async Task<PaymentDto> CompletePaymentAsync(
        Guid orderId,
        Guid paymentId,
        PaymentGatewayResult result,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdForUpdateAsync(orderId, cancellationToken)
            ?? throw new ConflictException("Payment result requires reconciliation.");
        var payment = order.Payments.SingleOrDefault(candidate => candidate.Id == paymentId)
            ?? throw new ConflictException("Payment attempt was not found.");
        if (payment.Status != PaymentStatus.Pending)
        {
            return payment.ToDto();
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            throw new ConflictException("Payment result requires reconciliation.");
        }

        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.TransactionId))
        {
            payment.MarkAsPaid(result.TransactionId);
            order.ChangeStatus(OrderStatus.Paid, _clock.UtcNow);
        }
        else
        {
            payment.MarkAsFailed("Payment provider rejected the payment attempt.");
        }

        await _notifications.QueuePaymentResultAsync(order, payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return payment.ToDto();
    }

    // Burada guest iade talebinin sahiplik, teslimat, miktar ve değişim varyantı kurallarını atomik denetliyorum.
    private async Task<ReturnRequestDto> CreateReturnInTransactionAsync(
        string sessionToken,
        string csrfToken,
        Guid orderId,
        ReturnType type,
        IReadOnlyList<CreateReturnItemCommand> items,
        string? customerNote,
        CancellationToken cancellationToken)
    {
        var session = await _access.ValidateSessionAsync(sessionToken, csrfToken, true, cancellationToken);
        var order = await _guestOrders.GetOrderForSessionAsync(session.Id, orderId, true, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (order.Status is not OrderStatus.Delivered and not OrderStatus.ReturnRequested and not OrderStatus.ReturnApproved)
        {
            throw new ConflictException("Only delivered orders or orders with an existing return can have a return request.");
        }

        if (items.Count == 0 || items.Select(item => item.OrderItemId).Distinct().Count() != items.Count)
        {
            throw new ConflictException("Return request must contain unique items.");
        }

        var previous = await _returns.GetByOrderIdForUpdateAsync(order.Id, cancellationToken);
        var consumed = previous.Where(request => request.ConsumesReturnQuantity())
            .SelectMany(request => request.Items)
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var orderItems = order.Items.ToDictionary(item => item.Id);
        var replacements = await ResolveReplacementsAsync(type, items, cancellationToken);
        var returnRequest = new ReturnRequest(order.Id, null, CreateReturnNumber(), type, customerNote);
        foreach (var item in items.OrderBy(item => item.OrderItemId))
        {
            if (!orderItems.TryGetValue(item.OrderItemId, out var orderItem))
            {
                throw new ConflictException("A return item does not belong to the selected order.");
            }

            var consumedQuantity = consumed.GetValueOrDefault(orderItem.Id);
            if (item.Quantity <= 0 || item.Quantity > orderItem.Quantity - consumedQuantity)
            {
                throw new ConflictException("Return quantity exceeds the remaining eligible quantity.");
            }

            ValidateReplacement(type, item, orderItem, replacements);
            var refund = type == ReturnType.Refund
                ? CalculateRefund(orderItem.RefundTotal, orderItem.Quantity, consumedQuantity, item.Quantity)
                : (decimal?)null;
            returnRequest.AddItem(orderItem, item.Quantity, item.ReplacementProductVariantId, refund);
        }

        await _returns.AddAsync(returnRequest, cancellationToken);
        order.MarkReturnRequested();
        await _notifications.QueueReturnRequestedAsync(returnRequest, order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return returnRequest.ToDto();
    }

    // Burada değişim talebindeki hedef varyantları tek sorguyla çözüyorum.
    private async Task<IReadOnlyDictionary<Guid, ProductVariant>> ResolveReplacementsAsync(
        ReturnType type,
        IReadOnlyCollection<CreateReturnItemCommand> items,
        CancellationToken cancellationToken)
    {
        if (type != ReturnType.Exchange)
        {
            return new Dictionary<Guid, ProductVariant>();
        }

        var ids = items.Where(item => item.ReplacementProductVariantId.HasValue)
            .Select(item => item.ReplacementProductVariantId!.Value).Distinct().ToList();
        return (await _variants.GetByIdsForUpdateAsync(ids, cancellationToken)).ToDictionary(variant => variant.Id);
    }

    // Burada değişim varyantının aynı ürün, farklı varyant, aktif, stoklu ve aynı fiyatlı olmasını doğruluyorum.
    private static void ValidateReplacement(
        ReturnType type,
        CreateReturnItemCommand item,
        OrderItem orderItem,
        IReadOnlyDictionary<Guid, ProductVariant> replacements)
    {
        if (type != ReturnType.Exchange)
        {
            return;
        }

        var id = item.ReplacementProductVariantId
            ?? throw new ConflictException("Every exchange item requires a replacement variant.");
        if (!replacements.TryGetValue(id, out var variant) || variant.Id == orderItem.ProductVariantId ||
            variant.ProductId != orderItem.ProductId || !variant.IsActive ||
            variant.Stock < item.Quantity || variant.NetPrice != orderItem.UnitPrice)
        {
            throw new ConflictException("Replacement product variant is not eligible.");
        }
    }

    // Burada kısmi iade tutarını önceki iadeleri de kapsayan deterministik yuvarlamayla hesaplıyorum.
    private static decimal CalculateRefund(decimal total, int ordered, int consumed, int requested)
    {
        var before = decimal.Round(total * consumed / ordered, OrderItem.SupportedPriceScale, MidpointRounding.AwayFromZero);
        var through = decimal.Round(total * (consumed + requested) / ordered, OrderItem.SupportedPriceScale, MidpointRounding.AwayFromZero);
        return through - before;
    }

    // Burada guest iade talebi için kısa ve benzersiz takip numarası üretiyorum.
    private static string CreateReturnNumber() => $"RET-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
}
