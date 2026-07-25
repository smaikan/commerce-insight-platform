using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Cart : AuditableEntity
{
    public const int MaximumSessionIdLength = 120;
    public const int MaximumDistinctItemCount = 100;

    private readonly List<CartItem> _items = [];

    public long? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public bool IsGuest => UserId is null;
    public bool IsEmpty => _items.Count == 0;
    public long TotalQuantity => _items.Sum(item => (long)item.Quantity);
    public decimal SubTotal => CalculateSubTotal();

    // Burada EF Core'un sepeti veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private Cart()
    {
    }

    // Burada sepetin yalnızca bir kullanıcıya veya bir misafir oturumuna ait olmasını sağlıyorum.
    private Cart(long? userId, string? sessionId)
    {
        if (userId.HasValue == !string.IsNullOrWhiteSpace(sessionId))
        {
            throw new DomainException("Cart must belong to either a user or a guest session.");
        }

        if (userId is <= 0)
        {
            throw new DomainException("User id must be greater than zero.");
        }

        var normalizedSessionId = sessionId?.Trim();
        if (normalizedSessionId?.Length > MaximumSessionIdLength)
        {
            throw new DomainException($"Session id cannot exceed {MaximumSessionIdLength} characters.");
        }

        UserId = userId;
        SessionId = normalizedSessionId;
        ConcurrencyToken = Guid.NewGuid();
    }

    // Burada kayıtlı kullanıcı için yeni ve boş bir sepet oluşturuyorum.
    public static Cart CreateForUser(long userId)
    {
        return new Cart(userId, null);
    }

    // Burada misafir oturumu için yeni ve boş bir sepet oluşturuyorum.
    public static Cart CreateForGuest(string sessionId)
    {
        return new Cart(null, sessionId);
    }

    // Burada ürünü sepete ekliyor, aynı varyant varsa adet ve fiyatı tek satırda birleştiriyorum.
    public CartItem AddItem(
        long productId,
        Guid productVariantId,
        int quantity,
        decimal unitPrice)
    {
        var existingItem = _items.SingleOrDefault(
            item => item.ProductVariantId == productVariantId);

        if (existingItem is not null)
        {
            var preview = existingItem.PreviewIncrease(productId, quantity, unitPrice);
            EnsureSubTotalCanChange(existingItem.TotalPrice, preview.TotalPrice);
            existingItem.ApplyValues(preview.Quantity, unitPrice);
            MarkAsChanged();
            return existingItem;
        }

        if (_items.Count >= MaximumDistinctItemCount)
        {
            throw new DomainException(
                $"Cart cannot contain more than {MaximumDistinctItemCount} distinct items.");
        }

        var item = new CartItem(this, productId, productVariantId, quantity, unitPrice);
        EnsureSubTotalCanChange(0m, item.TotalPrice);
        _items.Add(item);
        MarkAsChanged();
        return item;
    }

    // Burada sepetteki bir satırın adedini doğrulanmış biçimde değiştiriyorum.
    public void ChangeItemQuantity(Guid cartItemId, int quantity)
    {
        var item = GetItem(cartItemId);
        var updatedTotalPrice = CartItem.CalculateValidatedTotal(quantity, item.UnitPrice);
        EnsureSubTotalCanChange(item.TotalPrice, updatedTotalPrice);
        item.UpdateQuantity(quantity);
        MarkAsChanged();
    }

    // Burada sepette gösterilecek güncel birim fiyatı doğrulanmış biçimde değiştiriyorum.
    public void ChangeItemUnitPrice(Guid cartItemId, decimal unitPrice)
    {
        var item = GetItem(cartItemId);
        var updatedTotalPrice = CartItem.CalculateValidatedTotal(item.Quantity, unitPrice);
        EnsureSubTotalCanChange(item.TotalPrice, updatedTotalPrice);
        item.UpdateUnitPrice(unitPrice);
        MarkAsChanged();
    }

    // Burada bir sepet satırının adet ve güncel fiyatını tek atomik domain değişikliğiyle yeniliyorum.
    public void UpdateItem(Guid cartItemId, int quantity, decimal unitPrice)
    {
        var item = GetItem(cartItemId);
        var updatedTotalPrice = CartItem.CalculateValidatedTotal(quantity, unitPrice);
        EnsureSubTotalCanChange(item.TotalPrice, updatedTotalPrice);
        item.ApplyValues(quantity, unitPrice);
        MarkAsChanged();
    }

    // Burada belirtilen satırı sepetten kaldırıyorum.
    public void RemoveItem(Guid cartItemId)
    {
        var item = GetItem(cartItemId);
        _items.Remove(item);
        MarkAsChanged();
    }

    // Burada tüm satırları temizleyip boş sepette bile eşzamanlı yazmaları denetleyecek tokenı yeniliyorum.
    public void Clear()
    {
        _items.Clear();
        MarkAsChanged();
    }

    // Burada misafir sepetini kullanıcıya devredip oturum bağlantısını kaldırıyorum.
    public void AssignToUser(long userId)
    {
        if (userId <= 0)
        {
            throw new DomainException("User id must be greater than zero.");
        }

        if (UserId == userId)
        {
            return;
        }

        if (UserId.HasValue)
        {
            throw new DomainException("A registered cart cannot be assigned to another user.");
        }

        UserId = userId;
        SessionId = null;
        MarkAsChanged();
    }

    // Burada sepet satırını kimliğine göre buluyor, bulunamazsa geçersiz işlemi durduruyorum.
    private CartItem GetItem(Guid cartItemId)
    {
        if (cartItemId == Guid.Empty)
        {
            throw new DomainException("Cart item id is required.");
        }

        return _items.SingleOrDefault(item => item.Id == cartItemId)
            ?? throw new DomainException("Cart item was not found in this cart.");
    }

    // Burada mevcut sepet toplamını para sınırlarını aşmadan hesaplıyorum.
    private decimal CalculateSubTotal()
    {
        decimal subTotal = 0m;

        try
        {
            foreach (var item in _items)
            {
                subTotal = checked(subTotal + item.TotalPrice);
            }
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Cart subtotal exceeds the supported limit.", exception);
        }

        if (subTotal > CartItem.MaximumSupportedAmount)
        {
            throw new DomainException("Cart subtotal exceeds the supported monetary limit.");
        }

        return subTotal;
    }

    // Burada bir satır değişmeden önce oluşacak sepet toplamının desteklenen para alanına sığdığını doğruluyorum.
    private void EnsureSubTotalCanChange(decimal currentLineTotal, decimal updatedLineTotal)
    {
        decimal updatedSubTotal;

        try
        {
            updatedSubTotal = checked(CalculateSubTotal() - currentLineTotal + updatedLineTotal);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Cart subtotal exceeds the supported limit.", exception);
        }

        if (updatedSubTotal > CartItem.MaximumSupportedAmount)
        {
            throw new DomainException("Cart subtotal exceeds the supported monetary limit.");
        }
    }

    // Burada her sepet değişikliğinde concurrency ve güncellenme bilgilerini yeniliyorum.
    private void MarkAsChanged()
    {
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }
}
