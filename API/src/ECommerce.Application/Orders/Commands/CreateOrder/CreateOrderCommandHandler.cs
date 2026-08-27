using ECommerce.Application.Carts.Common;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IShippingMethodRepository? _shippingMethodRepository;
    private readonly IOrderMetricsRecorder _metricsRecorder;
    private readonly OrderCouponService _couponService;
    private readonly OrderPricingService _pricingService;
    private readonly IOrderNotificationService? _notificationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderReservationPolicy _reservationPolicy;
    private readonly OrderCheckoutOrchestrator? _checkoutOrchestrator;
    private readonly IAuthoritativeSalesMetricService? _salesMetrics;

    // Burada checkout akışının sepet, katalog, sipariş, metrik ve transaction bağımlılıklarını hazırlıyorum.
    public CreateOrderCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IOrderRepository orderRepository,
        IAddressRepository addressRepository,
        IOrderMetricsRecorder metricsRecorder,
        OrderCouponService couponService,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderReservationPolicy? reservationPolicy = null,
        IShippingMethodRepository? shippingMethodRepository = null,
        OrderPricingService? pricingService = null,
        IOrderNotificationService? notificationService = null,
        OrderCheckoutOrchestrator? checkoutOrchestrator = null,
        IAuthoritativeSalesMetricService? salesMetrics = null)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _orderRepository = orderRepository;
        _addressRepository = addressRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _metricsRecorder = metricsRecorder;
        _couponService = couponService;
        _pricingService = pricingService ?? new OrderPricingService();
        _notificationService = notificationService;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _reservationPolicy = reservationPolicy ?? DefaultOrderReservationPolicy.Instance;
        _checkoutOrchestrator = checkoutOrchestrator;
        _salesMetrics = salesMetrics;
    }

    // Burada oturum açmış kullanıcının sepetini serializable transaction içinde siparişe dönüştürüyorum.
    public Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        if (_checkoutOrchestrator is not null)
        {
            return _unitOfWork.ExecuteInSerializableTransactionAsync(
                transactionCancellationToken => CreateWithSharedOrchestratorAsync(
                    request,
                    userId,
                    transactionCancellationToken),
                cancellationToken);
        }

        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CreateInTransactionAsync(request, userId, transactionCancellationToken),
            cancellationToken);
    }

    // Burada production üye checkout'unu guest ile aynı ortak orkestratör üzerinden yürütüyorum.
    private async Task<OrderDto> CreateWithSharedOrchestratorAsync(
        CreateOrderCommand request,
        long userId,
        CancellationToken cancellationToken)
    {
        if (!request.ShippingAddressId.HasValue || !request.ShippingMethodId.HasValue)
        {
            throw new ConflictException("Shipping address and shipping method are required for checkout.");
        }

        var order = await _checkoutOrchestrator!.CreateAsync(
            new OrderCheckoutInput(
                CartOwner.ForUser(userId),
                userId,
                request.ExpectedCartConcurrencyToken,
                request.ShippingMethodId.Value,
                request.CouponCode,
                false,
                request.ShippingAddressId),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }

    // Burada stok, katalog snapshot'ı, envanter hareketi, metrikler ve sepet temizliğini atomik kaydediyorum.
    private async Task<OrderDto> CreateInTransactionAsync(
        CreateOrderCommand request,
        long userId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByOwnerForUpdateAsync(
            CartOwner.ForUser(userId),
            cancellationToken)
            ?? throw new NotFoundException("Cart was not found.");

        CartApplicationRules.EnsureExpectedConcurrencyToken(cart, request.ExpectedCartConcurrencyToken);
        if (cart.IsEmpty)
        {
            throw new ConflictException("Cart cannot be checked out because it is empty.");
        }

        var variants = await _variantRepository.GetByIdsForUpdateAsync(
            cart.Items.Select(item => item.ProductVariantId),
            cancellationToken);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        var products = await _productRepository.GetByIdsForUpdateAsync(
            variants.Select(variant => variant.ProductId),
            cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);
        var lines = new List<CheckoutLine>();
        decimal subTotal = 0m;
        foreach (var cartItem in cart.Items.OrderBy(item => item.ProductVariantId))
        {
            if (!variantsById.TryGetValue(cartItem.ProductVariantId, out var variant))
            {
                throw new ConflictException("A product variant in the cart is no longer available.");
            }

            if (!productsById.TryGetValue(variant.ProductId, out var product))
            {
                throw new ConflictException("A product in the cart is no longer available.");
            }

            if (cartItem.ProductId != product.Id)
            {
                throw new ConflictException("A cart item product does not match its selected variant.");
            }

            CartApplicationRules.EnsurePurchasable(product, variant);
            if (cartItem.Quantity > variant.Stock)
            {
                throw new ConflictException("Requested cart quantity exceeds available stock.");
            }

            decimal lineTotal;
            try
            {
                lineTotal = checked(variant.NetPrice * cartItem.Quantity);
                subTotal = checked(subTotal + lineTotal);
            }
            catch (OverflowException exception)
            {
                throw new ConflictException("Cart total exceeds the supported monetary limit.", exception);
            }

            lines.Add(new CheckoutLine(
                product,
                variant,
                cartItem.Quantity,
                product.TaxRate?.Rate ?? 0m));
        }

        var address = await ResolveShippingAddressAsync(request.ShippingAddressId, userId, cancellationToken);
        var shippingMethod = await ResolveShippingMethodAsync(
            request.ShippingMethodId,
            address,
            cancellationToken);
        var checkoutCoupon = await _couponService.ResolveForCheckoutAsync(
            request.CouponCode,
            subTotal,
            false,
            cancellationToken);
        var discountTotal = checkoutCoupon?.DiscountTotal ?? 0m;
        OrderPricingResult pricing;
        try
        {
            pricing = _pricingService.Calculate(
                lines.Select(line => new OrderPricingLine(
                    line.Variant.Id,
                    checked(line.Variant.NetPrice * line.Quantity),
                    line.TaxRatePercentage))
                    .ToList(),
                discountTotal,
                shippingMethod?.FixedFee ?? 0m);
        }
        catch (OverflowException exception)
        {
            throw new ConflictException("Cart total exceeds the supported monetary limit.", exception);
        }

        var order = new Order(
            userId,
            CreateOrderNumber(),
            pricing.SubTotal,
            pricing.DiscountTotal,
            pricing.ShippingTotal,
            pricing.TaxTotal,
            pricing.GrandTotal,
            address?.Id,
            checkoutCoupon?.Coupon.Code,
            shippingMethod?.Id,
            shippingMethod?.Name);
        if (address is not null)
        {
            order.SetShippingAddressSnapshot(address);
        }
        foreach (var line in lines)
        {
            var linePricing = pricing.Lines[line.Variant.Id];
            var mainImage = line.Product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .FirstOrDefault();
            order.AddItem(
                line.Product.Id,
                line.Variant.Id,
                line.Product.Title,
                line.Variant.Sku,
                line.Variant.NetPrice,
                line.Quantity,
                linePricing.DiscountTotal,
                linePricing.TaxRatePercentage,
                linePricing.TaxTotal,
                line.Product.Url,
                mainImage?.ImageUrl,
                mainImage?.AltText,
                line.Product.HasVariants ? line.Variant.Name : null,
                line.Product.HasVariants ? line.Variant.Value : null);
        }

        order.EnsureItemsMatchSubTotal();
        StartStockReservationWhenPaymentIsRequired(order);
        MarkAsPaidWhenNoPaymentIsRequired(order);
        if (order.Status == OrderStatus.Paid && _salesMetrics is not null)
        {
            await _salesMetrics.RecordPaidOrderAsync(order, cancellationToken);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _couponService.ConsumeAsync(checkoutCoupon, userId, order, cancellationToken);
        if (_notificationService is not null)
        {
            await _notificationService.QueueOrderCreatedAsync(order, cancellationToken);
        }

        foreach (var line in lines)
        {
            line.Variant.ApplyStockMovement(
                -line.Quantity,
                StockMovementType.Sale,
                "Order created.",
                order.Id);
        }

        await _metricsRecorder.RecordPurchasedQuantitiesAsync(
            lines.Select(line => new PurchaseMetricLine(line.Product, line.Variant, line.Quantity)).ToList(),
            cancellationToken);

        cart.Clear();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }

    // Burada isteğe bağlı teslimat adresinin yalnız oturum açmış kullanıcıya ait ve shipping türünde olduğunu doğruluyorum.
    private async Task<Address?> ResolveShippingAddressAsync(
        Guid? shippingAddressId,
        long userId,
        CancellationToken cancellationToken)
    {
        if (!shippingAddressId.HasValue)
        {
            return null;
        }

        var address = await _addressRepository.GetByIdForUserForUpdateAsync(
            shippingAddressId.Value,
            userId,
            cancellationToken)
            ?? throw new NotFoundException("Shipping address was not found.");
        if (address.Type != ECommerce.Domain.Enums.AddressType.Shipping)
        {
            throw new ConflictException("Only a shipping address can be selected for checkout.");
        }

        return address;
    }

    // Burada istemcinin kargo ücretini belirleyememesi için yalnız etkin ve takipli kargo yöntemi kaydını çözüyorum.
    private async Task<ShippingMethod?> ResolveShippingMethodAsync(
        Guid? shippingMethodId,
        Address? shippingAddress,
        CancellationToken cancellationToken)
    {
        if (!shippingMethodId.HasValue)
        {
            if (shippingAddress is not null)
            {
                throw new ConflictException("A shipping method is required when a shipping address is selected.");
            }

            return null;
        }

        if (shippingAddress is null)
        {
            throw new ConflictException("A shipping address is required when a shipping method is selected.");
        }

        if (_shippingMethodRepository is null)
        {
            throw new ConflictException("Shipping methods are not configured.");
        }

        var shippingMethod = await _shippingMethodRepository.GetByIdForUpdateAsync(
            shippingMethodId.Value,
            cancellationToken)
            ?? throw new NotFoundException("Shipping method was not found.");
        if (!shippingMethod.IsActive)
        {
            throw new ConflictException("The selected shipping method is not active.");
        }

        return shippingMethod;
    }

    // Burada GUID tabanlı, kısa ve insan tarafından ayırt edilebilir sipariş numarasını üretiyorum.
    private static string CreateOrderNumber()
    {
        return $"ORD-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
    }

    // Burada kupon indirimiyle ödeme gerektirmeyen siparişi ödeme kaydı oluşturmadan tamamlanmış kabul ediyorum.
    private void MarkAsPaidWhenNoPaymentIsRequired(Order order)
    {
        if (order.GrandTotal != 0m)
        {
            return;
        }

        order.ChangeStatus(OrderStatus.Confirmed, _clock.UtcNow);
        order.ChangeStatus(OrderStatus.Paid, _clock.UtcNow);
    }

    // Burada yalnız ödeme bekleyen siparişin stok düşümünü süreli rezervasyon olarak işaretliyorum.
    private void StartStockReservationWhenPaymentIsRequired(Order order)
    {
        if (order.GrandTotal == 0m)
        {
            return;
        }

        order.StartStockReservation(_clock.UtcNow, _reservationPolicy.ReservationDuration);
    }

    // Burada checkout sırasında aynı catalog kaydının güvenilir örneğini ve talep edilen adedini bir arada tutuyorum.
    private sealed record CheckoutLine(
        Product Product,
        ProductVariant Variant,
        int Quantity,
        decimal TaxRatePercentage);
}
