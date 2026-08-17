using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class IyzicoPaymentTests
{
    // Burada bekleyen iyzico ödemesinin hosted form oturumunu güvenli provider alanlarıyla sakladığını doğruluyorum.
    [Fact]
    public void InitializeCheckoutForm_Should_Persist_Session_On_Pending_Payment()
    {
        var payment = new Payment(
            Guid.NewGuid(),
            PaymentProvider.Iyzico,
            125.50m,
            "iyzico_domain_test_0001");
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        payment.InitializeCheckoutForm(
            "checkout-token-001",
            "conversation-001",
            "https://sandbox-api.iyzipay.com/checkoutform/token",
            expiresAt);

        payment.ProviderToken.Should().Be("checkout-token-001");
        payment.ProviderConversationId.Should().Be("conversation-001");
        payment.PaymentPageUrl.Should().StartWith("https://sandbox-api.iyzipay.com/");
        payment.ProviderTokenExpiresAt.Should().Be(expiresAt);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    // Burada javascript veya göreli ödeme sayfası adreslerinin domain'e giremediğini doğruluyorum.
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//sandbox-api.iyzipay.com/checkoutform/token")]
    [InlineData("/checkoutform/token")]
    public void InitializeCheckoutForm_Should_Reject_Non_Http_Absolute_Url(string paymentPageUrl)
    {
        var payment = new Payment(
            Guid.NewGuid(),
            PaymentProvider.Iyzico,
            10m,
            "iyzico_domain_test_0002");

        var action = () => payment.InitializeCheckoutForm(
            "checkout-token-002",
            "conversation-002",
            paymentPageUrl,
            DateTime.UtcNow.AddMinutes(30));

        action.Should().Throw<DomainException>();
    }

    // Burada doğrulanmış iyzico ödeme kimliği ve fraud durumunun Paid geçişinde korunduğunu doğruluyorum.
    [Fact]
    public void MarkAsPaid_Should_Record_Provider_Payment_And_Fraud_Status()
    {
        var payment = new Payment(
            Guid.NewGuid(),
            PaymentProvider.Iyzico,
            10m,
            "iyzico_domain_test_0003");

        payment.MarkAsPaid("28157797", 1);

        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.TransactionId.Should().Be("28157797");
        payment.FraudStatus.Should().Be(1);
        payment.PaidAt.Should().NotBeNull();
    }
}
