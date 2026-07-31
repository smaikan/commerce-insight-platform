using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using FluentAssertions;
using CartEntity = ECommerce.Domain.Entities.Cart;

namespace ECommerce.UnitTests.Domain.Cart;

public sealed class CartTests
{
    // Burada kayıtlı kullanıcı sepetinin yalnızca kullanıcı kimliğiyle oluştuğunu doğruluyorum.
    [Fact]
    public void CreateForUser_Should_Create_Registered_Cart()
    {
        var cart = CartEntity.CreateForUser(15);

        cart.UserId.Should().Be(15);
        cart.SessionId.Should().BeNull();
        cart.IsGuest.Should().BeFalse();
        cart.IsEmpty.Should().BeTrue();
        cart.ConcurrencyToken.Should().NotBeEmpty();
    }

    // Burada misafir oturum bilgisini temizleyerek doğru sahiplikle sepet oluşturuyorum.
    [Fact]
    public void CreateForGuest_Should_Trim_Session_Id()
    {
        var cart = CartEntity.CreateForGuest("  session-123  ");

        cart.UserId.Should().BeNull();
        cart.SessionId.Should().Be("session-123");
        cart.IsGuest.Should().BeTrue();
    }

    // Burada geçersiz kullanıcı kimliğiyle sepet oluşturulmasını engelliyorum.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateForUser_Should_Reject_Invalid_User_Id(long userId)
    {
        Action act = () => CartEntity.CreateForUser(userId);

        act.Should().Throw<DomainException>();
    }

    // Burada boş veya desteklenen uzunluğu aşan misafir oturumlarını reddediyorum.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateForGuest_Should_Reject_Empty_Session_Id(string sessionId)
    {
        Action act = () => CartEntity.CreateForGuest(sessionId);

        act.Should().Throw<DomainException>();
    }

    // Burada uzun oturum kimliğinin veritabanı sınırını aşmasını engelliyorum.
    [Fact]
    public void CreateForGuest_Should_Reject_Too_Long_Session_Id()
    {
        var sessionId = new string('a', CartEntity.MaximumSessionIdLength + 1);

        Action act = () => CartEntity.CreateForGuest(sessionId);

        act.Should().Throw<DomainException>();
    }

    // Burada yeni satırı sepete ekleyip toplam bilgilerini ve ilişkiyi oluşturuyorum.
    [Fact]
    public void AddItem_Should_Add_Item_And_Update_Cart_Summary()
    {
        var cart = CartEntity.CreateForUser(15);
        var originalConcurrencyToken = cart.ConcurrencyToken;
        var productVariantId = Guid.NewGuid();

        var item = cart.AddItem(20, productVariantId, 2, 75.25m);

        item.Cart.Should().BeSameAs(cart);
        item.CartId.Should().Be(cart.Id);
        item.ProductId.Should().Be(20);
        item.ProductVariantId.Should().Be(productVariantId);
        cart.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
        cart.TotalQuantity.Should().Be(2);
        cart.SubTotal.Should().Be(150.50m);
        cart.UpdatedAt.Should().NotBeNull();
        cart.ConcurrencyToken.Should().NotBe(originalConcurrencyToken);
    }

    // Burada aynı varyant tekrar eklendiğinde yeni satır açmadan adet ve fiyatı birleştiriyorum.
    [Fact]
    public void AddItem_Should_Merge_Duplicate_Variant()
    {
        var cart = CartEntity.CreateForUser(15);
        var productVariantId = Guid.NewGuid();
        var firstItem = cart.AddItem(20, productVariantId, 2, 75m);
        var concurrencyToken = cart.ConcurrencyToken;

        var mergedItem = cart.AddItem(20, productVariantId, 3, 80m);

        mergedItem.Should().BeSameAs(firstItem);
        cart.Items.Should().ContainSingle();
        mergedItem.Quantity.Should().Be(5);
        mergedItem.UnitPrice.Should().Be(80m);
        mergedItem.TotalPrice.Should().Be(400m);
        cart.ConcurrencyToken.Should().NotBe(concurrencyToken);
    }

    // Burada aynı varyantın farklı bir ürüne bağlanarak sepet bütünlüğünü bozmasını engelliyorum.
    [Fact]
    public void AddItem_Should_Reject_Duplicate_Variant_With_Different_Product()
    {
        var cart = CartEntity.CreateForUser(15);
        var productVariantId = Guid.NewGuid();
        var item = cart.AddItem(20, productVariantId, 2, 75m);
        var concurrencyToken = cart.ConcurrencyToken;

        Action act = () => cart.AddItem(21, productVariantId, 1, 90m);

        act.Should().Throw<DomainException>();
        cart.Items.Should().ContainSingle();
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(75m);
        cart.ConcurrencyToken.Should().Be(concurrencyToken);
    }

    // Burada satır adedi değiştiğinde sepet toplamı ve concurrency bilgisinin yenilendiğini doğruluyorum.
    [Fact]
    public void ChangeItemQuantity_Should_Update_Item_And_Cart()
    {
        var cart = CartEntity.CreateForUser(15);
        var item = cart.AddItem(20, Guid.NewGuid(), 2, 75m);
        var concurrencyToken = cart.ConcurrencyToken;

        cart.ChangeItemQuantity(item.Id, 4);

        item.Quantity.Should().Be(4);
        cart.TotalQuantity.Should().Be(4);
        cart.SubTotal.Should().Be(300m);
        cart.ConcurrencyToken.Should().NotBe(concurrencyToken);
    }

    // Burada geçersiz adet güncellemesinin satırı ve concurrency bilgisini değiştirmediğini doğruluyorum.
    [Fact]
    public void ChangeItemQuantity_Should_Keep_State_When_Quantity_Is_Invalid()
    {
        var cart = CartEntity.CreateForUser(15);
        var item = cart.AddItem(20, Guid.NewGuid(), 2, 75m);
        var concurrencyToken = cart.ConcurrencyToken;

        Action act = () => cart.ChangeItemQuantity(item.Id, 0);

        act.Should().Throw<DomainException>();
        item.Quantity.Should().Be(2);
        cart.ConcurrencyToken.Should().Be(concurrencyToken);
    }

    // Burada güvenilir güncel fiyat geldiğinde sepet satırının fiyatını yeniliyorum.
    [Fact]
    public void ChangeItemUnitPrice_Should_Update_Item_And_Total()
    {
        var cart = CartEntity.CreateForUser(15);
        var item = cart.AddItem(20, Guid.NewGuid(), 2, 75m);

        cart.ChangeItemUnitPrice(item.Id, 90m);

        item.UnitPrice.Should().Be(90m);
        cart.SubTotal.Should().Be(180m);
    }

    // Burada hassasiyeti geçersiz fiyatın satırı ve concurrency bilgisini değiştirmediğini doğruluyorum.
    [Fact]
    public void ChangeItemUnitPrice_Should_Keep_State_When_Price_Is_Invalid()
    {
        var cart = CartEntity.CreateForUser(15);
        var item = cart.AddItem(20, Guid.NewGuid(), 2, 75m);
        var concurrencyToken = cart.ConcurrencyToken;

        Action act = () => cart.ChangeItemUnitPrice(item.Id, 75.001m);

        act.Should().Throw<DomainException>();
        item.UnitPrice.Should().Be(75m);
        cart.SubTotal.Should().Be(150m);
        cart.ConcurrencyToken.Should().Be(concurrencyToken);
    }

    // Burada satırın adet ve fiyatını birlikte değiştirip concurrency bilgisini yalnız bir kez yeniliyorum.
    [Fact]
    public void UpdateItem_Should_Change_Quantity_And_UnitPrice_Atomically()
    {
        var cart = CartEntity.CreateForUser(15);
        var item = cart.AddItem(20, Guid.NewGuid(), 2, 75m);
        var concurrencyToken = cart.ConcurrencyToken;

        cart.UpdateItem(item.Id, 4, 80m);

        item.Quantity.Should().Be(4);
        item.UnitPrice.Should().Be(80m);
        item.TotalPrice.Should().Be(320m);
        cart.SubTotal.Should().Be(320m);
        cart.ConcurrencyToken.Should().NotBe(concurrencyToken);
    }

    // Burada birleşik satır güncellemesi geçersizse adet, fiyat ve concurrency bilgisini koruyorum.
    [Fact]
    public void UpdateItem_Should_Keep_State_When_New_Values_Are_Invalid()
    {
        var cart = CartEntity.CreateForUser(15);
        var item = cart.AddItem(20, Guid.NewGuid(), 2, 75m);
        var concurrencyToken = cart.ConcurrencyToken;

        Action act = () => cart.UpdateItem(item.Id, 0, 80m);

        act.Should().Throw<DomainException>();
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(75m);
        cart.ConcurrencyToken.Should().Be(concurrencyToken);
    }

    // Burada yeni satır sepet toplamını para sınırının üzerine çıkarırsa mevcut sepeti değiştirmiyorum.
    [Fact]
    public void AddItem_Should_Keep_State_When_Cart_SubTotal_Would_Exceed_Limit()
    {
        var cart = CartEntity.CreateForUser(15);
        var firstItem = cart.AddItem(20, Guid.NewGuid(), 1, 5_000_000_000_000_000m);
        var concurrencyToken = cart.ConcurrencyToken;

        Action act = () => cart.AddItem(
            productId: 21,
            productVariantId: Guid.NewGuid(),
            quantity: 1,
            unitPrice: 5_000_000_000_000_000m);

        act.Should().Throw<DomainException>();
        cart.Items.Should().ContainSingle().Which.Should().BeSameAs(firstItem);
        cart.SubTotal.Should().Be(5_000_000_000_000_000m);
        cart.ConcurrencyToken.Should().Be(concurrencyToken);
    }

    // Burada tek sepetin sınırsız büyüyerek sorgu ve cevap maliyetini artırmasını engelliyorum.
    [Fact]
    public void AddItem_Should_Reject_More_Than_Maximum_Distinct_Items()
    {
        var cart = CartEntity.CreateForUser(15);
        for (var index = 0; index < CartEntity.MaximumDistinctItemCount; index++)
        {
            cart.AddItem(index + 1, Guid.NewGuid(), 1, 1m);
        }

        Action act = () => cart.AddItem(
            CartEntity.MaximumDistinctItemCount + 1,
            Guid.NewGuid(),
            1,
            1m);

        act.Should().Throw<DomainException>();
        cart.Items.Should().HaveCount(CartEntity.MaximumDistinctItemCount);
    }

    // Burada seçilen satırı sepetten kaldırıp sepeti boş duruma getiriyorum.
    [Fact]
    public void RemoveItem_Should_Remove_Item()
    {
        var cart = CartEntity.CreateForUser(15);
        var item = cart.AddItem(20, Guid.NewGuid(), 2, 75m);

        cart.RemoveItem(item.Id);

        cart.Items.Should().BeEmpty();
        cart.IsEmpty.Should().BeTrue();
        cart.TotalQuantity.Should().Be(0);
        cart.SubTotal.Should().Be(0m);
    }

    // Burada sepete ait olmayan satır kimliğiyle değişiklik yapılmasını engelliyorum.
    [Fact]
    public void RemoveItem_Should_Reject_Unknown_Item()
    {
        var cart = CartEntity.CreateForUser(15);

        Action act = () => cart.RemoveItem(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    // Burada dolu sepetin tüm satırlarını temizliyorum.
    [Fact]
    public void Clear_Should_Remove_All_Items()
    {
        var cart = CartEntity.CreateForUser(15);
        cart.AddItem(20, Guid.NewGuid(), 2, 75m);
        cart.AddItem(21, Guid.NewGuid(), 1, 50m);
        var concurrencyToken = cart.ConcurrencyToken;

        cart.Clear();

        cart.Items.Should().BeEmpty();
        cart.TotalQuantity.Should().Be(0);
        cart.SubTotal.Should().Be(0m);
        cart.ConcurrencyToken.Should().NotBe(concurrencyToken);
    }

    // Burada boş sepet temizliğinde de eşzamanlı değişiklik kontrolü için tokenı yeniliyorum.
    [Fact]
    public void Clear_Should_Refresh_Concurrency_Token_When_Cart_Is_Already_Empty()
    {
        var cart = CartEntity.CreateForUser(15);
        var concurrencyToken = cart.ConcurrencyToken;

        cart.Clear();

        cart.Items.Should().BeEmpty();
        cart.ConcurrencyToken.Should().NotBe(concurrencyToken);
    }

    // Burada misafir sepetini kullanıcıya bağlayıp oturum kimliğini kaldırıyorum.
    [Fact]
    public void AssignToUser_Should_Convert_Guest_Cart()
    {
        var cart = CartEntity.CreateForGuest("session-123");
        var concurrencyToken = cart.ConcurrencyToken;

        cart.AssignToUser(15);

        cart.UserId.Should().Be(15);
        cart.SessionId.Should().BeNull();
        cart.IsGuest.Should().BeFalse();
        cart.ConcurrencyToken.Should().NotBe(concurrencyToken);
    }

    // Burada kayıtlı bir sepetin başka kullanıcıya devredilmesini engelliyorum.
    [Fact]
    public void AssignToUser_Should_Reject_Different_Registered_User()
    {
        var cart = CartEntity.CreateForUser(15);

        Action act = () => cart.AssignToUser(16);

        act.Should().Throw<DomainException>();
        cart.UserId.Should().Be(15);
    }

    // Burada dışarıya açılan item listesinin doğrudan değiştirilemediğini doğruluyorum.
    [Fact]
    public void Items_Should_Be_Read_Only()
    {
        var cart = CartEntity.CreateForUser(15);
        cart.AddItem(20, Guid.NewGuid(), 1, 75m);
        var exposedItems = cart.Items.Should()
            .BeAssignableTo<ICollection<CartItem>>()
            .Subject;

        Action act = exposedItems.Clear;

        act.Should().Throw<NotSupportedException>();
        cart.Items.Should().ContainSingle();
    }
}
