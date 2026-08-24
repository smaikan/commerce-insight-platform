using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Order : AuditableEntity
{
    public const int MaximumOrderNumberLength = 30;
    public const int MaximumItemCount = 100;
    public const int MaximumCouponCodeLength = 50;
    public const int MaximumShippingCarrierLength = 150;
    public const int MaximumTrackingNumberLength = 100;
    public const int MaximumTrackingUrlLength = 500;
    public const decimal MaximumSupportedAmount = OrderItem.MaximumSupportedAmount;
    public static readonly TimeSpan MaximumStockReservationDuration = TimeSpan.FromDays(7);

    private readonly List<OrderItem> _items = [];
    private readonly List<Payment> _payments = [];
    private readonly List<OrderAddressSnapshot> _addressSnapshots = [];

    public long? UserId { get; private set; }
    public string OrderNumber { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal ShippingTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public Guid? AddressId { get; private set; }
    public Address? Address { get; private set; }
    public string? CouponCode { get; private set; }
    public Guid? ShippingMethodId { get; private set; }
    public ShippingMethod? ShippingMethod { get; private set; }
    public string? ShippingMethodName { get; private set; }
    public DateTime? ReservationExpiresAt { get; private set; }
    public OrderCustomerSnapshot? CustomerSnapshot { get; private set; }
    public IReadOnlyCollection<OrderAddressSnapshot> AddressSnapshots => _addressSnapshots.AsReadOnly();
    public OrderAddressSnapshot? ShippingAddressSnapshot => _addressSnapshots.SingleOrDefault(snapshot => snapshot.Type == AddressType.Shipping);
    public OrderAddressSnapshot? BillingAddressSnapshot => _addressSnapshots.SingleOrDefault(snapshot => snapshot.Type == AddressType.Billing);
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();
    public DateTime? PaidAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? ShippingCarrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? TrackingUrl { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    // Burada EF Core'un sipariş aggregate'ını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private Order()
    {
    }

    // Burada siparişin kullanıcı, numara ve parasal özet kurallarını koruyarak aggregate'ı oluşturuyorum.
    public Order(
        long? userId,
        string orderNumber,
        decimal subTotal,
        decimal discountTotal,
        decimal shippingTotal,
        decimal taxTotal,
        decimal grandTotal,
        Guid? addressId = null,
        string? couponCode = null,
        Guid? shippingMethodId = null,
        string? shippingMethodName = null)
    {
        if (userId.HasValue && userId.Value <= 0)
        {
            throw new DomainException("User id must be positive when supplied.");
        }

        OrderNumber = NormalizeOrderNumber(orderNumber);
        ValidateTotals(subTotal, discountTotal, shippingTotal, taxTotal, grandTotal);

        UserId = userId;
        Status = OrderStatus.Pending;
        SubTotal = subTotal;
        DiscountTotal = discountTotal;
        ShippingTotal = shippingTotal;
        TaxTotal = taxTotal;
        GrandTotal = grandTotal;
        AddressId = addressId == Guid.Empty
            ? throw new DomainException("Address id cannot be empty.")
            : addressId;
        CouponCode = NormalizeCouponCode(couponCode);
        ShippingMethodId = shippingMethodId == Guid.Empty
            ? throw new DomainException("Shipping method id cannot be empty.")
            : shippingMethodId;
        ShippingMethodName = NormalizeShippingMethodName(shippingMethodName);
        EnsureShippingMethodConsistency();
    }

    // Burada güvenilir katalog snapshot'ıyla yeni sipariş kalemini aggregate'a ekliyorum.
    public OrderItem AddItem(
        long productId,
        Guid productVariantId,
        string productTitleSnapshot,
        string variantSkuSnapshot,
        decimal unitPrice,
        int quantity,
        decimal discountTotal = 0m,
        decimal taxRatePercentage = 0m,
        decimal taxTotal = 0m,
        string? productUrlSnapshot = null,
        string? imageUrlSnapshot = null,
        string? imageAltSnapshot = null,
        string? variantNameSnapshot = null,
        string? variantValueSnapshot = null)
    {
        if (_items.Count >= MaximumItemCount)
        {
            throw new DomainException($"Order cannot contain more than {MaximumItemCount} items.");
        }

        if (_items.Any(item => item.ProductVariantId == productVariantId))
        {
            throw new DomainException("Order cannot contain the same product variant more than once.");
        }

        var item = new OrderItem(
            this,
            productId,
            productVariantId,
            productTitleSnapshot,
            variantSkuSnapshot,
            unitPrice,
            quantity,
            discountTotal,
            taxRatePercentage,
            taxTotal,
            productUrlSnapshot,
            imageUrlSnapshot,
            imageAltSnapshot,
            variantNameSnapshot,
            variantValueSnapshot);
        decimal updatedItemTotal;
        try
        {
            updatedItemTotal = checked(CalculateItemTotal() + item.TotalPrice);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Order item total exceeds the supported limit.", exception);
        }

        if (updatedItemTotal > SubTotal)
        {
            throw new DomainException("Order item total cannot exceed order subtotal.");
        }

        _items.Add(item);
        MarkAsUpdated();
        return item;
    }

    // Burada güvenilir teslimat adresinin sipariş anındaki değerlerini yalnız bir kez snapshot olarak saklıyorum.
    public void SetShippingAddressSnapshot(Address address)
    {
        if (address is null || !AddressId.HasValue || AddressId.Value != address.Id)
        {
            throw new DomainException("The shipping address must match the order address id.");
        }

        AddAddressSnapshot(new OrderAddressSnapshot(this, address, AddressType.Shipping));
    }

    // Burada üye veya guest siparişinin zorunlu müşteri iletişim snapshot'ını yalnız bir kez oluşturuyorum.
    public void SetCustomerSnapshot(string firstName, string lastName, string email, string phoneNumber)
    {
        if (CustomerSnapshot is not null)
        {
            throw new DomainException("Order customer snapshot is already set.");
        }

        CustomerSnapshot = new OrderCustomerSnapshot(this, firstName, lastName, email, phoneNumber);
        MarkAsUpdated();
    }

    // Burada guest checkout teslimat adresini kaynak kullanıcı adresi olmadan snapshot olarak saklıyorum.
    public void SetGuestShippingAddressSnapshot(
        string title,
        string firstName,
        string lastName,
        string phoneNumber,
        string city,
        string district,
        string? neighborhood,
        string fullAddress,
        string? postalCode)
    {
        AddAddressSnapshot(new OrderAddressSnapshot(
            this,
            null,
            AddressType.Shipping,
            title,
            firstName,
            lastName,
            phoneNumber,
            city,
            district,
            neighborhood,
            fullAddress,
            postalCode));
    }

    // Burada ayrı fatura adresini kaynak adresi olsa da olmasa da değişmez snapshot olarak saklıyorum.
    public void SetBillingAddressSnapshot(
        Guid? sourceAddressId,
        string title,
        string firstName,
        string lastName,
        string phoneNumber,
        string city,
        string district,
        string? neighborhood,
        string fullAddress,
        string? postalCode)
    {
        AddAddressSnapshot(new OrderAddressSnapshot(
            this,
            sourceAddressId,
            AddressType.Billing,
            title,
            firstName,
            lastName,
            phoneNumber,
            city,
            district,
            neighborhood,
            fullAddress,
            postalCode));
    }

    // Burada guest sipariş claim edildiğinde kullanıcı bağını yalnız sahipsiz siparişe atıyorum.
    public void Claim(long userId)
    {
        if (userId <= 0)
        {
            throw new DomainException("Claim user id must be positive.");
        }

        if (UserId.HasValue && UserId.Value != userId)
        {
            throw new DomainException("Order is already owned by another user.");
        }

        UserId = userId;
        MarkAsUpdated();
    }

    // Burada aynı tipte ikinci adres snapshot'ı oluşmasını engelleyerek aggregate koleksiyonuna ekliyorum.
    private void AddAddressSnapshot(OrderAddressSnapshot snapshot)
    {
        if (_addressSnapshots.Any(existing => existing.Type == snapshot.Type))
        {
            throw new DomainException($"Order {snapshot.Type} address snapshot is already set.");
        }

        _addressSnapshots.Add(snapshot);
        MarkAsUpdated();
    }

    // Burada ödeme kaydını doğru sipariş ve toplam tutarı koruyarak sipariş aggregate'ına ekliyorum.
    public void AddPayment(Payment payment)
    {
        if (payment is null || payment.OrderId != Id)
        {
            throw new DomainException("Payment must belong to this order.");
        }

        if (payment.Amount != GrandTotal)
        {
            throw new DomainException("Payment amount must match the order grand total.");
        }

        if (_payments.Any(existingPayment => existingPayment.Id == payment.Id))
        {
            throw new DomainException("Payment is already attached to this order.");
        }

        _payments.Add(payment);
        MarkAsUpdated();
    }

    // Burada kalem snapshot toplamının siparişin güvenilir subtotal değeriyle tamamen eşleştiğini doğruluyorum.
    public void EnsureItemsMatchSubTotal()
    {
        if (_items.Count == 0)
        {
            throw new DomainException("Order must contain at least one item.");
        }

        if (CalculateItemTotal() != SubTotal)
        {
            throw new DomainException("Order items are not consistent with the order subtotal.");
        }

        if (CalculateItemDiscountTotal() != DiscountTotal)
        {
            throw new DomainException("Order items are not consistent with the order discount total.");
        }

        if (CalculateItemTaxTotal() != TaxTotal)
        {
            throw new DomainException("Order items are not consistent with the order tax total.");
        }
    }

    // Burada pozitif tutarlı siparişin ayrılmış stoğunu güvenilir UTC süreyle ödeme sonuna kadar bağlıyorum.
    public void StartStockReservation(DateTime utcNow, TimeSpan reservationDuration)
    {
        EnsureUtc(utcNow, "Stock reservation time");
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Stock reservation can only start for a pending order.");
        }

        if (GrandTotal <= 0m)
        {
            throw new DomainException("A zero-total order does not require a stock reservation.");
        }

        EnsureItemsMatchSubTotal();
        if (reservationDuration <= TimeSpan.Zero || reservationDuration > MaximumStockReservationDuration)
        {
            throw new DomainException("Stock reservation duration is outside the supported range.");
        }

        try
        {
            ReservationExpiresAt = utcNow.Add(reservationDuration);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new DomainException("Stock reservation expiry is outside the supported range.", exception);
        }

        MarkAsUpdated();
    }

    // Burada siparişin güvenli olarak otomatik iptal edilebilecek süresi geçmiş stok rezervasyonu olup olmadığını denetliyorum.
    public bool CanExpireStockReservation(DateTime utcNow)
    {
        EnsureUtc(utcNow, "Stock reservation expiration time");
        return ReservationExpiresAt.HasValue &&
               ReservationExpiresAt.Value <= utcNow &&
               Status is OrderStatus.Pending or OrderStatus.Confirmed &&
               !_payments.Any(payment => payment.Status is PaymentStatus.Paid or PaymentStatus.Pending);
    }

    // Burada yalnız güvenli olarak sona erdirilebilen rezervasyonu sipariş iptaline çevirip çağrıyı idempotent tutuyorum.
    public bool ExpireStockReservation(DateTime utcNow)
    {
        if (!CanExpireStockReservation(utcNow))
        {
            return false;
        }

        ChangeStatus(OrderStatus.Cancelled, utcNow);
        return true;
    }

    // Burada yeni iade talebini sipariş durumuna yansıtıp teslim edilmiş sipariş bağını koruyorum.
    public void MarkReturnRequested()
    {
        if (Status == OrderStatus.ReturnRequested)
        {
            return;
        }

        if (Status is not OrderStatus.Delivered and not OrderStatus.ReturnApproved)
        {
            throw new DomainException("A return request can only be opened for a delivered or returned order.");
        }

        Status = OrderStatus.ReturnRequested;
        MarkAsUpdated();
    }

    // Burada yöneticinin onayladığı iade talebini sipariş durumunda görünür hale getiriyorum.
    public void MarkReturnApproved()
    {
        if (Status == OrderStatus.ReturnApproved)
        {
            return;
        }

        if (Status is not OrderStatus.Delivered and not OrderStatus.ReturnRequested)
        {
            throw new DomainException("A return can only be approved for a delivered order with a return request.");
        }

        Status = OrderStatus.ReturnApproved;
        MarkAsUpdated();
    }

    // Burada onaylanan ücret iadesini siparişin kalıcı iş durumu olarak kaydediyorum.
    public void MarkRefunded()
    {
        if (Status == OrderStatus.Refunded)
        {
            return;
        }

        if (Status is not OrderStatus.Delivered and not OrderStatus.ReturnRequested and not OrderStatus.ReturnApproved)
        {
            throw new DomainException("A refund can only be approved for a delivered order with a return request.");
        }

        Status = OrderStatus.Refunded;
        MarkAsUpdated();
    }

    // Burada aktif iade kalmadığında siparişi teslim edilmiş durumuna geri getiriyorum.
    public void RestoreDeliveredAfterReturnResolution()
    {
        if (Status == OrderStatus.Delivered)
        {
            return;
        }

        if (Status is not OrderStatus.ReturnRequested and not OrderStatus.ReturnApproved)
        {
            throw new DomainException("Only return-related orders can be restored to delivered.");
        }

        Status = OrderStatus.Delivered;
        MarkAsUpdated();
    }

    // Burada siparişin geçerli yaşam döngüsü geçişini uygulayıp ilgili tarih alanlarını güncelliyorum.
    public void ChangeStatus(OrderStatus status, DateTime utcNow)
    {
        EnsureUtc(utcNow, "Order status change time");

        if (!CanTransitionTo(status))
        {
            throw new DomainException($"Order status cannot change from {Status} to {status}.");
        }

        if (status == OrderStatus.Paid &&
            GrandTotal > 0m &&
            !_payments.Any(payment => payment.Status == PaymentStatus.Paid))
        {
            throw new DomainException("A successful payment is required before the order can be marked as paid.");
        }

        Status = status;
        if (status == OrderStatus.Paid)
        {
            PaidAt = utcNow;
            ReservationExpiresAt = null;
        }

        if (status == OrderStatus.Cancelled)
        {
            CancelledAt = utcNow;
            ReservationExpiresAt = null;
        }

        if (status == OrderStatus.Shipped && !ShippedAt.HasValue)
        {
            ShippedAt = utcNow;
        }

        if (status == OrderStatus.Delivered && !DeliveredAt.HasValue)
        {
            DeliveredAt = utcNow;
        }

        MarkAsUpdated();
    }

    // Burada provider ters işlemi başlamadan önce sipariş satırını transaction yarışlarında görünür bir cancellation intent yazımına hazırlıyorum.
    public void RegisterCancellationIntent()
    {
        if (Status is not OrderStatus.Paid and not OrderStatus.Preparing)
        {
            throw new DomainException("Only a paid or preparing order can register a cancellation intent.");
        }

        MarkAsUpdated();
    }

    // Burada kargo takip snapshot'ını hazırlanan veya kargodaki sipariş için doğrulayıp kargoya çıkış anını koruyorum.
    public void SetShipment(string shippingCarrier, string trackingNumber, string? trackingUrl, DateTime utcNow)
    {
        EnsureUtc(utcNow, "Shipment update time");
        if (Status is not OrderStatus.Preparing and not OrderStatus.Shipped)
        {
            throw new DomainException("Shipment can only be set for a preparing or shipped order.");
        }

        var normalizedCarrier = NormalizeRequiredShipmentValue(
            shippingCarrier,
            MaximumShippingCarrierLength,
            "Shipping carrier");
        var normalizedTrackingNumber = NormalizeRequiredShipmentValue(
            trackingNumber,
            MaximumTrackingNumberLength,
            "Tracking number");
        var normalizedTrackingUrl = NormalizeTrackingUrl(trackingUrl);

        ShippingCarrier = normalizedCarrier;
        TrackingNumber = normalizedTrackingNumber;
        TrackingUrl = normalizedTrackingUrl;

        if (Status == OrderStatus.Preparing)
        {
            ChangeStatus(OrderStatus.Shipped, utcNow);
            return;
        }

        MarkAsUpdated();
    }

    // Burada zorunlu kargo takip metnini boşluk ve uzunluk sınırıyla normalize ediyorum.
    private static string NormalizeRequiredShipmentValue(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    // Burada takip bağlantısını yalnız güvenli mutlak HTTP veya HTTPS adresi olarak kabul ediyorum.
    private static string? NormalizeTrackingUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > MaximumTrackingUrlLength ||
            !Uri.TryCreate(normalizedValue, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException("Tracking URL must be an absolute HTTP or HTTPS URL within the supported length.");
        }

        return normalizedValue;
    }

    // Burada yalnızca içe aktarma akışının, geçmişte oluşmuş siparişin UTC oluşma anını korumasını sağlıyorum.
    public void SetImportedCreatedAt(DateTime createdAtUtc)
    {
        EnsureUtc(createdAtUtc, "Imported order creation time");
        CreatedAt = createdAtUtc;
    }

    // Burada hedef sipariş durumunun mevcut durumdan izin verilen bir geçiş olup olmadığını denetliyorum.
    private bool CanTransitionTo(OrderStatus targetStatus)
    {
        return Status switch
        {
            OrderStatus.Pending => targetStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => targetStatus is OrderStatus.Paid or OrderStatus.Cancelled,
            OrderStatus.Paid => targetStatus is OrderStatus.Preparing or OrderStatus.Cancelled or OrderStatus.Refunded,
            OrderStatus.Preparing => targetStatus is OrderStatus.Shipped or OrderStatus.Cancelled or OrderStatus.Refunded,
            OrderStatus.Shipped => targetStatus is OrderStatus.Delivered or OrderStatus.Refunded,
            OrderStatus.Delivered => targetStatus == OrderStatus.Refunded,
            OrderStatus.Cancelled or OrderStatus.Refunded => false,
            _ => false
        };
    }

    // Burada sipariş akışına giren zaman değerinin UTC olmasını merkezi olarak doğruluyorum.
    private static void EnsureUtc(DateTime utcNow, string fieldName)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }

    // Burada sipariş kalemlerinin hesaplanabilir toplamını para taşmasına izin vermeden üretiyorum.
    private decimal CalculateItemTotal()
    {
        decimal total = 0m;

        try
        {
            foreach (var item in _items)
            {
                total = checked(total + item.TotalPrice);
            }
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Order item total exceeds the supported limit.", exception);
        }

        return total;
    }

    // Burada kalemlerde snapshot olarak saklanan indirimleri taşma denetimiyle sipariş indirim toplamına bağlıyorum.
    private decimal CalculateItemDiscountTotal()
    {
        decimal total = 0m;

        try
        {
            foreach (var item in _items)
            {
                total = checked(total + item.DiscountTotal);
            }
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Order item discount total exceeds the supported limit.", exception);
        }

        return total;
    }

    // Burada kalemlerde snapshot olarak saklanan vergileri taşma denetimiyle sipariş vergi toplamına bağlıyorum.
    private decimal CalculateItemTaxTotal()
    {
        decimal total = 0m;

        try
        {
            foreach (var item in _items)
            {
                total = checked(total + item.TaxTotal);
            }
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Order item tax total exceeds the supported limit.", exception);
        }

        return total;
    }

    // Burada sipariş numarasını veritabanı sözleşmesine uygun, boş olmayan sınırlı bir değere dönüştürüyorum.
    private static string NormalizeOrderNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("Order number cannot be empty.");
        }

        var normalizedOrderNumber = orderNumber.Trim();
        if (normalizedOrderNumber.Length > MaximumOrderNumberLength)
        {
            throw new DomainException($"Order number cannot exceed {MaximumOrderNumberLength} characters.");
        }

        return normalizedOrderNumber;
    }

    // Burada isteğe bağlı kupon kodunu arşiv amaçlı büyük harfli ve sınırlı biçimde saklıyorum.
    private static string? NormalizeCouponCode(string? couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return null;
        }

        var normalizedCouponCode = couponCode.Trim().ToUpperInvariant();
        if (normalizedCouponCode.Length > MaximumCouponCodeLength)
        {
            throw new DomainException($"Coupon code cannot exceed {MaximumCouponCodeLength} characters.");
        }

        return normalizedCouponCode;
    }

    // Burada seçili kargo yönteminin sipariş anındaki adını boşluk ve uzunluk kurallarına göre snapshot olarak saklıyorum.
    private static string? NormalizeShippingMethodName(string? shippingMethodName)
    {
        if (string.IsNullOrWhiteSpace(shippingMethodName))
        {
            return null;
        }

        var normalizedShippingMethodName = shippingMethodName.Trim();
        if (normalizedShippingMethodName.Length > ShippingMethod.MaximumNameLength)
        {
            throw new DomainException($"Shipping method name cannot exceed {ShippingMethod.MaximumNameLength} characters.");
        }

        return normalizedShippingMethodName;
    }

    // Burada kargo ücretinin yalnız güvenilir yöntem seçimiyle birlikte tutulduğunu doğruluyorum.
    private void EnsureShippingMethodConsistency()
    {
        if (ShippingMethodId.HasValue != (ShippingMethodName is not null))
        {
            throw new DomainException("Shipping method id and name must be provided together.");
        }

        if (ShippingTotal > 0m && !ShippingMethodId.HasValue)
        {
            throw new DomainException("A shipping method is required when a shipping fee is charged.");
        }
    }

    // Burada sipariş toplamlarının negatif olmamasını ve grand total formülünü doğruluyorum.
    private static void ValidateTotals(
        decimal subTotal,
        decimal discountTotal,
        decimal shippingTotal,
        decimal taxTotal,
        decimal grandTotal)
    {
        if (subTotal < 0 || discountTotal < 0 || shippingTotal < 0 || taxTotal < 0 || grandTotal < 0)
        {
            throw new DomainException("Order totals cannot be negative.");
        }

        if (subTotal > MaximumSupportedAmount ||
            discountTotal > MaximumSupportedAmount ||
            shippingTotal > MaximumSupportedAmount ||
            taxTotal > MaximumSupportedAmount ||
            grandTotal > MaximumSupportedAmount)
        {
            throw new DomainException("Order totals exceed the supported monetary limit.");
        }

        if (discountTotal > subTotal)
        {
            throw new DomainException("Order discount total cannot exceed subtotal.");
        }

        decimal expectedGrandTotal;
        try
        {
            expectedGrandTotal = checked(subTotal - discountTotal + shippingTotal + taxTotal);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Order grand total exceeds the supported limit.", exception);
        }

        if (grandTotal != expectedGrandTotal)
        {
            throw new DomainException("Order grand total is not consistent with order totals.");
        }
    }
}
