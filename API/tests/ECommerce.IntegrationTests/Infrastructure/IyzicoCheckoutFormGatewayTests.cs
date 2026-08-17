using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Application.Common.Payments;
using ECommerce.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.IntegrationTests.Infrastructure;

public sealed class IyzicoCheckoutFormGatewayTests
{
    private const string ApiKey = "sandbox-api-key";
    private const string SecretKey = "sandbox-secret-key";

    // Burada initialize isteğinin hosted form alanlarını, IYZWSv2 headerını ve yanıt imzasını doğruluyorum.
    [Fact]
    public async Task InitializeAsync_Should_Send_Signed_Request_And_Validate_Response()
    {
        const string token = "checkout-token-001";
        const string conversationId = "conversation-001";
        string? capturedAuthorization = null;
        string? capturedBody = null;
        var signature = SignResponse(conversationId, token);
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedAuthorization = request.Headers.Authorization?.ToString();
            capturedBody = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, new
            {
                status = "success",
                conversationId,
                token,
                paymentPageUrl = "https://sandbox-cpp.iyzipay.com?token=checkout-token-001&lang=tr",
                tokenExpireTime = 1800,
                signature
            });
        });
        var gateway = CreateGateway(handler);

        var result = await gateway.InitializeAsync(CreateInitializeRequest(conversationId));

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be(token);
        result.PaymentPageUrl.Should().StartWith("https://sandbox-cpp.iyzipay.com");
        capturedAuthorization.Should().StartWith("IYZWSv2 ");
        var decodedAuthorization = Encoding.UTF8.GetString(Convert.FromBase64String(
            capturedAuthorization!["IYZWSv2 ".Length..]));
        decodedAuthorization.Should().Contain($"apiKey:{ApiKey}");
        decodedAuthorization.Should().Contain("randomKey:");
        decodedAuthorization.Should().Contain("signature:");
        using var body = JsonDocument.Parse(capturedBody!);
        body.RootElement.GetProperty("currency").GetString().Should().Be("TRY");
        body.RootElement.GetProperty("callbackUrl").GetString().Should()
            .Be("https://api.example.com/api/payments/iyzico/callback");
        body.RootElement.GetProperty("basketItems").GetArrayLength().Should().Be(1);
        body.RootElement.GetProperty("buyer").TryGetProperty("identityNumber", out _).Should().BeTrue();
    }

    // Burada bozulmuş initialize imzasının ödeme sayfası URL'si olarak kabul edilmediğini doğruluyorum.
    [Fact]
    public async Task InitializeAsync_Should_Reject_Tampered_Response_Signature()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            conversationId = "conversation-001",
            token = "checkout-token-001",
            paymentPageUrl = "https://sandbox-cpp.iyzipay.com?token=checkout-token-001&lang=tr",
            tokenExpireTime = 1800,
            signature = new string('0', 64)
        })));
        var gateway = CreateGateway(handler);

        var action = () => gateway.InitializeAsync(CreateInitializeRequest("conversation-001"));

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*integrity validation failed*");
    }

    // Burada imzası doğru olsa bile sandbox origin'i dışındaki ödeme URL'sinin reddedildiğini doğruluyorum.
    [Fact]
    public async Task InitializeAsync_Should_Reject_Unexpected_Payment_Page_Origin()
    {
        const string conversationId = "conversation-001";
        const string token = "checkout-token-001";
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            conversationId,
            token,
            paymentPageUrl = "https://evil.example/checkoutform/token",
            tokenExpireTime = 1800,
            signature = SignResponse(conversationId, token)
        })));
        var gateway = CreateGateway(handler);

        var action = () => gateway.InitializeAsync(CreateInitializeRequest(conversationId));

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*integrity validation failed*");
    }

    // Burada retrieve yanıtının resmi alan sırasıyla imzalanıp Paid sonucuna dönüştürüldüğünü doğruluyorum.
    [Fact]
    public async Task RetrieveAsync_Should_Validate_Signature_And_Return_Paid()
    {
        const string paymentStatus = "SUCCESS";
        const string paymentId = "12345678";
        const string currency = "TRY";
        const string basketId = "basket-001";
        const string conversationId = "conversation-001";
        const string token = "checkout-token-001";
        const decimal paidPrice = 125.50m;
        const decimal price = 100m;
        var signature = SignResponse(
            paymentStatus,
            paymentId,
            currency,
            basketId,
            conversationId,
            "125.5",
            "100",
            token);
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            conversationId,
            price,
            paidPrice,
            currency,
            basketId,
            paymentId,
            paymentStatus,
            fraudStatus = 1,
            token,
            signature
        }))));

        var result = await gateway.RetrieveAsync(token);

        result.State.Should().Be(CheckoutFormPaymentState.Paid);
        result.ProviderPaymentId.Should().Be(paymentId);
        result.FraudStatus.Should().Be(1);
        result.Price.Should().Be(price);
        result.PaidPrice.Should().Be(paidPrice);
    }

    // Burada yalnız güncel HPP V3 webhook alan sırasıyla üretilen imzanın kabul edildiğini doğruluyorum.
    [Fact]
    public void ValidateWebhookSignature_Should_Use_Hpp_V3_Order()
    {
        var gateway = CreateGateway(new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent))));
        var notification = new CheckoutFormWebhookNotification(
            "CHECKOUT_FORM_AUTH",
            "28157797",
            "checkout-token-001",
            "conversation-001",
            "SUCCESS");
        var signature = SignRaw(string.Concat(
            SecretKey,
            notification.EventType,
            notification.ProviderPaymentId,
            notification.Token,
            notification.PaymentConversationId,
            notification.Status));

        gateway.ValidateWebhookSignature(notification, signature).Should().BeTrue();
        gateway.ValidateWebhookSignature(notification, new string('f', 64)).Should().BeFalse();
    }

    // Burada etkin sandbox ayarlarında credential ve URL doğrulamasının başlangıçta başarılı olduğunu doğruluyorum.
    [Fact]
    public void OptionsValidator_Should_Accept_Complete_Sandbox_Configuration()
    {
        var result = new IyzicoOptionsValidator().Validate(null, CreateOptions());

        result.Succeeded.Should().BeTrue();
    }

    // Burada iyzico adapterını sabit sandbox ayarları ve gözlemlenebilir HTTP handlerıyla oluşturuyorum.
    private static IyzicoCheckoutFormGateway CreateGateway(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://sandbox-api.iyzipay.com")
        };
        return new IyzicoCheckoutFormGateway(
            client,
            Options.Create(CreateOptions()),
            NullLogger<IyzicoCheckoutFormGateway>.Instance);
    }

    // Burada testte kullanılacak eksiksiz ve secret sızdırmayan sandbox seçeneklerini oluşturuyorum.
    private static IyzicoOptions CreateOptions()
    {
        return new IyzicoOptions
        {
            Enabled = true,
            BaseUrl = "https://sandbox-api.iyzipay.com",
            ApiKey = ApiKey,
            SecretKey = SecretKey,
            CallbackUrl = "https://api.example.com/api/payments/iyzico/callback",
            ReturnUrl = "https://store.example.com/checkout/payment-result",
            SandboxBuyerIdentityNumber = "11111111111",
            Country = "Turkey",
            EnabledInstallments = [1, 2, 3, 6, 9, 12]
        };
    }

    // Burada initialize testi için müşteri, adres ve basket snapshot'ı oluşturuyorum.
    private static CheckoutFormInitializeGatewayRequest CreateInitializeRequest(string conversationId)
    {
        return new CheckoutFormInitializeGatewayRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            conversationId,
            "basket-001",
            100m,
            125.50m,
            "127.0.0.1",
            new CheckoutFormBuyer("buyer-001", "Ada", "Lovelace", "ada@example.com", "+905551112233"),
            new CheckoutFormAddress("Ada Lovelace", "Istanbul", "Kadikoy", "Test address", "34000"),
            new CheckoutFormAddress("Ada Lovelace", "Istanbul", "Kadikoy", "Test address", "34000"),
            [new CheckoutFormBasketItem("item-001", "Test Product", 100m)]);
    }

    // Burada resmi iki nokta ayraçlı iyzico yanıt imzasını test için üretiyorum.
    private static string SignResponse(params string[] values)
    {
        return SignRaw(string.Join(':', values));
    }

    // Burada HMACSHA256 test imzasını küçük harfli hex biçiminde üretiyorum.
    private static string SignRaw(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    // Burada test HTTP yanıtını camelCase JSON içerikle oluşturuyorum.
    private static HttpResponseMessage Json(HttpStatusCode statusCode, object value)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        // Burada HTTP davranışını test senaryosunun callback'iyle yapılandırıyorum.
        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        // Burada gerçek ağa çıkmadan iyzico isteğini gözlemleyip sahte yanıt döndürüyorum.
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
