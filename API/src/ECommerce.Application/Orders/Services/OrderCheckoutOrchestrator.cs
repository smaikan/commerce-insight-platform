using ECommerce.Application.Carts.Common;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.Services;

public sealed class OrderCheckoutOrchestrator
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderMetricsRecorder _metricsRecorder;
    private readonly OrderCouponService _couponService;
    private readonly OrderPricingService _pricingService;
    private readonly IOrderNotificationService _notificationService;
    private readonly IDateTimeProvider _clock;
    private readonly IOrderReservationPolicy _reservationPolicy;
    private readonly IAuthoritativeSalesMetricService _salesMetrics;

    // Burada üye ve guest checkout'un ortak güvenilir sipariş oluşturma bağımlılıklarını hazırlıyorum.
    public OrderCheckoutOrchestrator(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IOrderRepository orderRepository,
        IAddressRepository addressRepository,
        IShippingMethodRepository shippingMethodRepository,
        IUserRepository userRepository,
        IOrderMetricsRecorder metricsRecorder,
        OrderCouponService couponService,
        OrderPricingService pricingService,
        IOrderNotificationService notificationService,
        IDateTimeProvider clock,
        IOrderReservationPolicy reservationPolicy,
        IAuthoritativeSalesMetricService salesMetrics)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _orderRepository = orderRepository;
        _addressRepository = addressRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _userRepository = userRepository;
        _metricsRecorder = metricsRecorder;
        _couponService = couponService;
        _pricingService = pricingService;
        _notificationService = notificationService;
        _clock = clock;
        _reservationPolicy = reservationPolicy;
        _salesMetrics = salesMetrics;
    }

    // Burada fiyat, kargo, kupon, stok ve snapshot kurallarını tek checkout akışında atomik kayda hazırlıyorum.
    public async Task<Order> CreateAsync(OrderCheckoutInput input, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByOwnerForUpdateAsync(input.Owner, cancellationToken)
            ?? throw new NotFoundException("Cart was not found.");
        CartApplicationRules.EnsureExpectedConcurrencyToken(cart, input.ExpectedCartConcurrencyToken);
        if (cart.IsEmpty)
        {
            throw new ConflictException("Cart cannot be checked out because it is empty.");
        }

        var lines = await ResolveLinesAsync(cart, cancellationToken);
        var subTotal = CalculateSubTotal(lines);
        var checkoutIdentity = await ResolveIdentityAsync(input, cancellationToken);
        var shippingMethod = await _shippingMethodRepository.GetByIdForUpdateAsync(input.ShippingMethodId, cancellationToken)
            ?? throw new NotFoundException("Shipping method was not found.");
        if (!shippingMethod.IsActive)
        {
            throw new ConflictException("The selected shipping method is not active.");
        }

        var checkoutCoupon = await _couponService.ResolveForCheckoutAsync(
            input.CouponCode,
            subTotal,
            input.IsGuest,
            cancellationToken);
        var pricing = CalculatePricing(lines, checkoutCoupon?.DiscountTotal ?? 0m, shippingMethod.FixedFee);
        var order = new Order(
            input.UserId,
            CreateOrderNumber(),
            pricing.SubTotal,
            pricing.DiscountTotal,
            pricing.ShippingTotal,
            pricing.TaxTotal,
            pricing.GrandTotal,
            checkoutIdentity.AddressId,
            checkoutCoupon?.Coupon.Code,
            shippingMethod.Id,
            shippingMethod.Name);

        order.SetCustomerSnapshot(
            checkoutIdentity.Customer.FirstName,
            checkoutIdentity.Customer.LastName,
            checkoutIdentity.Customer.Email,
            checkoutIdentity.Customer.PhoneNumber);
        if (checkoutIdentity.RegisteredShippingAddress is not null)
        {
            order.SetShippingAddressSnapshot(checkoutIdentity.RegisteredShippingAddress);
        }
        else
        {
            SetGuestShippingSnapshot(order, checkoutIdentity.ShippingAddress);
        }

        SetBillingSnapshot(order, checkoutIdentity.BillingAddress);
        AddItems(order, lines, pricing);
        order.EnsureItemsMatchSubTotal();
        StartReservationOrCompleteFreeOrder(order);
        if (order.Status == OrderStatus.Paid)
        {
            await _salesMetrics.RecordPaidOrderAsync(order, cancellationToken);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _couponService.ConsumeAsync(checkoutCoupon, input.UserId, order, cancellationToken);
        if (order.Status == OrderStatus.Paid)
        {
            await _notificationService.QueueOrderCreatedAsync(order, cancellationToken);
        }
        await ApplyStockAndMetricsAsync(order, lines, cancellationToken);
        return order;
    }

    // Burada sepet satırlarını güncel ve kilitli katalog kayıtlarıyla doğruluyorum.
    private async Task<IReadOnlyList<CheckoutLine>> ResolveLinesAsync(Cart cart, CancellationToken cancellationToken)
    {
        var variants = await _variantRepository.GetByIdsForUpdateAsync(
            cart.Items.Select(item => item.ProductVariantId), cancellationToken);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        var products = await _productRepository.GetByIdsForUpdateAsync(
            variants.Select(variant => variant.ProductId), cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);
        var lines = new List<CheckoutLine>();

        foreach (var item in cart.Items.OrderBy(item => item.ProductVariantId))
        {
            if (!variantsById.TryGetValue(item.ProductVariantId, out var variant) ||
                !productsById.TryGetValue(variant.ProductId, out var product) ||
                item.ProductId != product.Id)
            {
                throw new ConflictException("A product in the cart is no longer available.");
            }

            CartApplicationRules.EnsurePurchasable(product, variant);
            if (item.Quantity > variant.Stock)
            {
                throw new ConflictException("Requested cart quantity exceeds available stock.");
            }

            lines.Add(new CheckoutLine(product, variant, item.Quantity, product.TaxRate?.Rate ?? 0m));
        }

        return lines;
    }

    // Burada sepetin güvenilir net fiyat ara toplamını taşma denetimiyle hesaplıyorum.
    private static decimal CalculateSubTotal(IReadOnlyList<CheckoutLine> lines)
    {
        decimal subTotal = 0m;
        try
        {
            foreach (var line in lines)
            {
                subTotal = checked(subTotal + line.Variant.NetPrice * line.Quantity);
            }
        }
        catch (OverflowException exception)
        {
            throw new ConflictException("Cart total exceeds the supported monetary limit.", exception);
        }

        return subTotal;
    }

    // Burada üye adresi ve kullanıcı kaydını ya da guest snapshot girdilerini tek modele dönüştürüyorum.
    private async Task<ResolvedCheckoutIdentity> ResolveIdentityAsync(OrderCheckoutInput input, CancellationToken cancellationToken)
    {
        if (input.IsGuest)
        {
            if (input.UserId.HasValue || input.GuestCustomer is null || input.GuestShippingAddress is null)
            {
                throw new ConflictException("Guest checkout customer and shipping address are required.");
            }

            var billing = input.GuestBillingAddress ?? input.GuestShippingAddress with { Type = AddressType.Billing };
            return new ResolvedCheckoutIdentity(null, null, input.GuestCustomer, input.GuestShippingAddress, billing);
        }

        if (!input.UserId.HasValue || !input.ShippingAddressId.HasValue)
        {
            throw new ConflictException("Shipping address is required for checkout.");
        }

        var address = await _addressRepository.GetByIdForUserForUpdateAsync(
            input.ShippingAddressId.Value, input.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Shipping address was not found.");
        if (address.Type != AddressType.Shipping)
        {
            throw new ConflictException("Only a shipping address can be selected for checkout.");
        }

        var user = await _userRepository.GetByIdAsync(input.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Checkout user was not found.");
        var customer = new CheckoutCustomerInput(
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber ?? address.PhoneNumber);
        var snapshot = CheckoutAddressInput.FromAddress(address);
        return new ResolvedCheckoutIdentity(address.Id, address, customer, snapshot, snapshot with { Type = AddressType.Billing });
    }

    // Burada merkezi fiyatlandırma servisiyle satır indirimi, vergi ve toplamları hesaplıyorum.
    private OrderPricingResult CalculatePricing(IReadOnlyList<CheckoutLine> lines, decimal discount, decimal shipping)
    {
        try
        {
            return _pricingService.Calculate(
                lines.Select(line => new OrderPricingLine(
                    line.Variant.Id,
                    checked(line.Variant.NetPrice * line.Quantity),
                    line.TaxRatePercentage)).ToList(),
                discount,
                shipping);
        }
        catch (OverflowException exception)
        {
            throw new ConflictException("Cart total exceeds the supported monetary limit.", exception);
        }
    }

    // Burada guest teslimat adresini sipariş aggregate'ına ekliyorum.
    private static void SetGuestShippingSnapshot(Order order, CheckoutAddressInput address)
    {
        order.SetGuestShippingAddressSnapshot(address.Title, address.FirstName, address.LastName, address.PhoneNumber, address.City, address.District, address.Neighborhood, address.FullAddress, address.PostalCode);
    }

    // Burada zorunlu billing snapshot'ını ayrı adres veya teslimat fallback'iyle ekliyorum.
    private static void SetBillingSnapshot(Order order, CheckoutAddressInput address)
    {
        order.SetBillingAddressSnapshot(address.SourceAddressId, address.Title, address.FirstName, address.LastName, address.PhoneNumber, address.City, address.District, address.Neighborhood, address.FullAddress, address.PostalCode);
    }

    // Burada güvenilir katalog ve fiyat snapshot'larını sipariş kalemlerine ekliyorum.
    private static void AddItems(Order order, IReadOnlyList<CheckoutLine> lines, OrderPricingResult pricing)
    {
        foreach (var line in lines)
        {
            var linePricing = pricing.Lines[line.Variant.Id];
            var mainImage = line.Product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .FirstOrDefault();
            order.AddItem(
                line.Product.Id, line.Variant.Id, line.Product.Title, line.Variant.Sku,
                line.Variant.NetPrice, line.Quantity, linePricing.DiscountTotal,
                linePricing.TaxRatePercentage, linePricing.TaxTotal,
                line.Product.Url, mainImage?.ImageUrl, mainImage?.AltText,
                line.Product.HasVariants ? line.Variant.Name : null,
                line.Product.HasVariants ? line.Variant.Value : null);
        }
    }

    // Burada ödeme gereken siparişe rezervasyon, sıfır toplamlı siparişe tamamlanmış ödeme durumu uyguluyorum.
    private void StartReservationOrCompleteFreeOrder(Order order)
    {
        if (order.GrandTotal == 0m)
        {
            order.ChangeStatus(OrderStatus.Confirmed, _clock.UtcNow);
            order.ChangeStatus(OrderStatus.Paid, _clock.UtcNow);
            return;
        }

        order.StartStockReservation(_clock.UtcNow, _reservationPolicy.ReservationDuration);
    }

    // Burada her varyant için yalnız mevcut Sale hareketi yolunu kullanıp satın alma metriğini kaydediyorum.
    private async Task ApplyStockAndMetricsAsync(Order order, IReadOnlyList<CheckoutLine> lines, CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            line.Variant.ApplyStockMovement(-line.Quantity, StockMovementType.Sale, "Order created.", order.Id);
        }

        await _metricsRecorder.RecordPurchasedQuantitiesAsync(
            lines.Select(line => new PurchaseMetricLine(line.Product, line.Variant, line.Quantity)).ToList(),
            cancellationToken);
    }

    // Burada insan tarafından ayırt edilebilir benzersiz sipariş numarasını üretiyorum.
    private static string CreateOrderNumber() => $"ORD-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

    // Burada checkout sırasında güvenilir katalog nesneleriyle adedi birlikte taşıyorum.
    private sealed record CheckoutLine(Product Product, ProductVariant Variant, int Quantity, decimal TaxRatePercentage);

    // Burada çözülmüş müşteri ve adres snapshot kaynaklarını birlikte taşıyorum.
    private sealed record ResolvedCheckoutIdentity(
        Guid? AddressId,
        Address? RegisteredShippingAddress,
        CheckoutCustomerInput Customer,
        CheckoutAddressInput ShippingAddress,
        CheckoutAddressInput BillingAddress);
}

// Burada üye ve guest checkout için ortak orkestratör girdisini tanımlıyorum.
public sealed record OrderCheckoutInput(
    CartOwner Owner,
    long? UserId,
    Guid ExpectedCartConcurrencyToken,
    Guid ShippingMethodId,
    string? CouponCode,
    bool IsGuest,
    Guid? ShippingAddressId = null,
    CheckoutCustomerInput? GuestCustomer = null,
    CheckoutAddressInput? GuestShippingAddress = null,
    CheckoutAddressInput? GuestBillingAddress = null);

// Burada guest müşteri snapshot'ının zorunlu alanlarını taşıyorum.
public sealed record CheckoutCustomerInput(string FirstName, string LastName, string Email, string PhoneNumber);

// Burada frontend fiyat alanı içermeyen checkout adres snapshot girdisini taşıyorum.
public sealed record CheckoutAddressInput(
    Guid? SourceAddressId,
    AddressType Type,
    string Title,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District, string? Neighborhood,
    string FullAddress,
    string? PostalCode)
{
    // Burada kayıtlı kullanıcı adresini ortak checkout snapshot girdisine dönüştürüyorum.
    public static CheckoutAddressInput FromAddress(Address address) => new(
        address.Id, address.Type, address.Title, address.FirstName, address.LastName,
        address.PhoneNumber, address.City, address.District, address.Neighborhood, address.FullAddress, address.PostalCode);
}
