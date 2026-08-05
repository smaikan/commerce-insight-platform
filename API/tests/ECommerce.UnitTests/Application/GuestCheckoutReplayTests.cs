using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.GuestOrders.Checkout;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class GuestCheckoutReplayTests
{
    // Burada kaybolan ilk checkout cevabından sonra aynı idempotency anahtarıyla yapılan tekrarın yeni erişim oturumu ve grant ürettiğini doğruluyorum.
    [Fact]
    public async Task Replay_Without_Existing_Session_Should_Create_Session_And_Access_Grant()
    {
        var now = new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(null, "ORD-REPLAY-1", 100m, 0m, 0m, 20m, 120m,
            shippingMethodId: Guid.NewGuid(), shippingMethodName: "Standart");
        var record = new GuestCheckoutIdempotency(
            Hash("cart"), Hash("key"), Hash(CreateFingerprint()), order, now, now.AddHours(24));
        var repository = new Mock<IGuestOrderRepository>();
        repository.Setup(item => item.GetIdempotencyForUpdateAsync(Hash("cart"), Hash("key"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        repository.Setup(item => item.AddSessionAsync(It.IsAny<GuestOrderSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(item => item.GetAccessGrantForUpdateAsync(It.IsAny<Guid>(), order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuestOrderAccessGrant?)null);
        repository.Setup(item => item.AddAccessGrantAsync(It.IsAny<GuestOrderAccessGrant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tokens = new Mock<IGuestTokenService>();
        tokens.Setup(item => item.Hash(It.IsAny<string>())).Returns((string value) => Hash(value));
        tokens.SetupSequence(item => item.CreateToken())
            .Returns(new GuestSecurityToken("session-raw", Hash("session-raw")))
            .Returns(new GuestSecurityToken("csrf-raw", Hash("csrf-raw")));
        var protection = new Mock<IGuestCheckoutProtectionService>();
        var unitOfWork = CreateTransactionalUnitOfWork();
        var handler = new CreateGuestOrderCommandHandler(
            null!, repository.Object, new Mock<IEmailOutboxRepository>().Object, tokens.Object,
            new Mock<IGuestOrderAccessTokenProtector>().Object, protection.Object, new FixedClock(now), unitOfWork.Object);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.WasReplay.Should().BeTrue();
        result.Order.Id.Should().Be(order.Id);
        result.NewSessionToken.Should().Be("session-raw");
        result.NewCsrfToken.Should().Be("csrf-raw");
        result.SessionExpiresAt.Should().Be(now.AddDays(7));
        repository.Verify(item => item.AddAccessGrantAsync(
            It.Is<GuestOrderAccessGrant>(grant => grant.OrderId == order.Id), It.IsAny<CancellationToken>()), Times.Once);
        protection.Verify(item => item.EvaluateCheckoutAsync(It.IsAny<GuestCheckoutProtectionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada test checkout isteğinin idempotency parmak iziyle üretimdeki kurala eşdeğer olmasını sağlıyorum.
    private static CreateGuestOrderCommand CreateCommand() => new(
        "cart", null, "192.0.2.1", null, "key", Guid.Parse("5f9c71b8-3535-4768-b67c-9036b194fcea"),
        new CheckoutCustomerInput("Ada", "Lovelace", "ada@example.com", "+905551112233"),
        new CheckoutAddressInput(null, AddressType.Shipping, "Ev", "Ada", "Lovelace", "+905551112233", "Istanbul", "Kadikoy", "Ornek Sokak 1", "34000"),
        null, Guid.Parse("8d4d2a3d-8535-42fa-8528-896536562a5b"), "welcome");

    // Burada üretimdeki PII saklamayan istek parmak iziyle aynı sıralı metni hazırlıyorum.
    private static string CreateFingerprint() => string.Join('|',
        "5f9c71b835354768b67c9036b194fcea", "Ada", "Lovelace", "ada@example.com", "+905551112233",
        "0~Ev~Ada~Lovelace~+905551112233~Istanbul~Kadikoy~Ornek Sokak 1~34000", "billing=fallback",
        "8d4d2a3d853542fa8528896536562a5b", "WELCOME");

    // Burada testin kullandığı hash değerini token servisinin güvenli üretim kuralıyla uyumlu hazırlıyorum.
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    // Burada serializable transaction delegesini testte doğrudan çalıştıran unit of work mockunu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<GuestCheckoutResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<GuestCheckoutResult>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        private readonly DateTime _now;

        // Burada replay testi için sabit UTC zamanını hazırlıyorum.
        public FixedClock(DateTime now)
        {
            _now = now;
        }

        public DateTime UtcNow => _now;
    }
}
