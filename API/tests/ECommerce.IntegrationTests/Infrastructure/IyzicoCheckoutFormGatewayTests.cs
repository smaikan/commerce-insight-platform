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

    // Burada token, conversation ve imzası doğrulanan initialize reddinin kesin başarısızlık olarak ayrıldığını doğruluyorum.
    [Fact]
    public async Task InitializeAsync_Should_Return_Definitive_Failure_Only_For_Signed_Bound_Response()
    {
        const string conversationId = "conversation-failed-001";
        const string token = "checkout-token-failed-001";
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "failure",
            conversationId,
            token,
            errorCode = "10051",
            errorMessage = "Provider rejected request.",
            signature = SignResponse(conversationId, token)
        }))));

        var result = await gateway.InitializeAsync(CreateInitializeRequest(conversationId));

        result.Succeeded.Should().BeFalse();
        result.IsDefinitiveFailure.Should().BeTrue();
        result.Token.Should().Be(token);
        result.ConversationId.Should().Be(conversationId);
    }

    // Burada imzasız initialize reddinin kesin başarısızlık kabul edilip rezervasyonu bırakamadığını doğruluyorum.
    [Fact]
    public async Task InitializeAsync_Should_Reject_Unsigned_Failure_Response()
    {
        const string conversationId = "conversation-failed-002";
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "failure",
            conversationId,
            token = "checkout-token-failed-002",
            errorCode = "10051"
        }))));

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
        string? capturedBody = null;
        var signature = SignResponse(
            paymentStatus,
            paymentId,
            currency,
            basketId,
            conversationId,
            "125.5",
            "100",
            token);
        var gateway = CreateGateway(new StubHttpMessageHandler(async request =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, new
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
                installment = 6,
                token,
                signature
            });
        }));

        var result = await gateway.RetrieveAsync(token, conversationId);

        result.State.Should().Be(CheckoutFormPaymentState.Paid);
        result.ProviderPaymentId.Should().Be(paymentId);
        result.FraudStatus.Should().Be(1);
        result.Price.Should().Be(price);
        result.PaidPrice.Should().Be(paidPrice);
        result.InstallmentCount.Should().Be(6);
        using var requestBody = JsonDocument.Parse(capturedBody!);
        requestBody.RootElement.GetProperty("conversationId").GetString().Should().Be(conversationId);
    }

    // Burada imzalı FAILURE yanıtının kimliği doğrulandıktan sonra kesin başarısızlığa dönüştürüldüğünü doğruluyorum.
    [Fact]
    public async Task RetrieveAsync_Should_Return_Failed_For_Signed_Provider_Failure()
    {
        const string paymentStatus = "FAILURE";
        const string paymentId = "failed-payment-001";
        const string currency = "TRY";
        const string basketId = "basket-failed-001";
        const string conversationId = "conversation-failed-003";
        const string token = "checkout-token-failed-003";
        var signature = SignResponse(
            paymentStatus,
            paymentId,
            currency,
            basketId,
            conversationId,
            "100",
            "100",
            token);
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            conversationId,
            price = 100m,
            paidPrice = 100m,
            currency,
            basketId,
            paymentId,
            paymentStatus,
            fraudStatus = -1,
            installment = 1,
            token,
            signature
        }))));

        var result = await gateway.RetrieveAsync(token, conversationId);

        result.State.Should().Be(CheckoutFormPaymentState.Failed);
        result.Token.Should().Be(token);
        result.ConversationId.Should().Be(conversationId);
    }

    // Burada fraud incelemesindeki sıfır durumunun imzalı olsa bile kesin başarısızlığa çevrilmediğini doğruluyorum.
    [Fact]
    public async Task RetrieveAsync_Should_Return_Pending_For_FraudStatus_Zero()
    {
        const string conversationId = "conversation-pending-001";
        const string token = "checkout-token-pending-001";
        var signature = SignResponse(
            "FAILURE", "pending-payment-001", "TRY", "basket-pending-001",
            conversationId, "100", "100", token);
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            conversationId,
            price = 100m,
            paidPrice = 100m,
            currency = "TRY",
            basketId = "basket-pending-001",
            paymentId = "pending-payment-001",
            paymentStatus = "FAILURE",
            fraudStatus = 0,
            installment = 1,
            token,
            signature
        }))));

        var result = await gateway.RetrieveAsync(token, conversationId);

        result.State.Should().Be(CheckoutFormPaymentState.Pending);
        result.FraudStatus.Should().Be(0);
    }

    // Burada süresi dolmuş token benzeri API-level failure yanıtının finansal sonuç yerine özel provider ret hatasına dönüştüğünü doğruluyorum.
    [Fact]
    public async Task RetrieveAsync_Should_Separate_Api_Level_Failure_From_Payment_Result()
    {
        const string conversationId = "conversation-expired-001";
        const string token = "checkout-token-expired-001";
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "failure",
            conversationId,
            errorCode = "5122"
        }))));

        var action = () => gateway.RetrieveAsync(token, conversationId);

        var exception = await action.Should().ThrowAsync<CheckoutFormProviderUnavailableException>();
        exception.Which.ErrorCode.Should().Be("5122");
    }

    // Burada geç tahsilat ters işleminin paymentId ile imzalı cancel endpointine gönderildiğini ve kimlik/tutar yanıtının doğrulandığını test ediyorum.
    [Fact]
    public async Task ReverseLatePaymentAsync_Should_Send_Cancel_Request_And_Validate_Result()
    {
        const string providerPaymentId = "late-payment-001";
        const string conversationId = "abandon-conversation-001";
        string? capturedPath = null;
        string? capturedAuthorization = null;
        string? capturedBody = null;
        var gateway = CreateGateway(new StubHttpMessageHandler(async request =>
        {
            capturedPath = request.RequestUri?.AbsolutePath;
            capturedAuthorization = request.Headers.Authorization?.ToString();
            capturedBody = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, new
            {
                status = "success",
                conversationId,
                paymentId = providerPaymentId,
                price = 100m,
                currency = "TRY"
            });
        }));

        var result = await gateway.ReverseLatePaymentAsync(
            providerPaymentId,
            conversationId,
            100m);

        result.Succeeded.Should().BeTrue();
        capturedPath.Should().Be("/payment/cancel");
        capturedAuthorization.Should().StartWith("IYZWSv2 ");
        using var body = JsonDocument.Parse(capturedBody!);
        body.RootElement.GetProperty("paymentId").GetString().Should().Be(providerPaymentId);
        body.RootElement.GetProperty("conversationId").GetString().Should().Be(conversationId);
    }

    // Burada standart item refund isteğinin Refund V2 yerine paymentTransactionId ile gönderilip resmi response HMAC'ini doğruladığını test ediyorum.
    [Fact]
    public async Task RefundPaymentItemAsync_Should_Send_Item_Level_Request_And_Validate_Signature()
    {
        const string providerPaymentId = "refund-payment-001";
        const string providerTransactionId = "refund-item-001";
        const string conversationId = "refund-conversation-001";
        string? capturedPath = null;
        string? capturedBody = null;
        var signature = SignResponse(providerPaymentId, "44", "TRY", conversationId);
        var gateway = CreateGateway(new StubHttpMessageHandler(async request =>
        {
            capturedPath = request.RequestUri?.AbsolutePath;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, new
            {
                status = "success",
                conversationId,
                paymentId = providerPaymentId,
                paymentTransactionId = providerTransactionId,
                price = 44m,
                currency = "TRY",
                signature
            });
        }));

        var result = await gateway.RefundPaymentItemAsync(
            providerPaymentId,
            providerTransactionId,
            conversationId,
            44m);

        result.Succeeded.Should().BeTrue();
        capturedPath.Should().Be("/payment/refund");
        using var body = JsonDocument.Parse(capturedBody!);
        body.RootElement.GetProperty("paymentTransactionId").GetString().Should().Be(providerTransactionId);
        body.RootElement.GetProperty("price").GetDecimal().Should().Be(44m);
        body.RootElement.TryGetProperty("paymentId", out _).Should().BeFalse();
    }

    // Burada reporting cevabındaki payment, cancel ve item refund kimliklerinin reconciliation modeline eksiksiz taşındığını doğruluyorum.
    [Fact]
    public async Task RetrieveReversalReportAsync_Should_Map_Authoritative_Reversal_Evidence()
    {
        const string providerPaymentId = "12345678";
        string? capturedPathAndQuery = null;
        var gateway = CreateGateway(new StubHttpMessageHandler(request =>
        {
            capturedPathAndQuery = request.RequestUri?.PathAndQuery;
            return Task.FromResult(Json(HttpStatusCode.OK, new
            {
                status = "success",
                payments = new[]
                {
                    new
                    {
                        paymentId = 12345678,
                        paymentConversationId = "original-payment-conversation",
                        paymentRefundStatus = "PARTIALLY_REFUNDED",
                        basketId = "basket-001",
                        currency = "TRY",
                        price = 100m,
                        paidPrice = 110m,
                        cancels = Array.Empty<object>(),
                        itemTransactions = new[]
                        {
                            new
                            {
                                paymentTransactionId = 27225633,
                                price = 40m,
                                paidPrice = 44m,
                                refunds = new[]
                                {
                                    new
                                    {
                                        refundConversationId = "refund-conversation-001",
                                        refundPrice = 44m,
                                        refundStatus = 1,
                                        currencyCode = "TRY"
                                    }
                                }
                            }
                        }
                    }
                }
            }));
        }));

        var report = await gateway.RetrieveReversalReportAsync(providerPaymentId);

        capturedPathAndQuery.Should().Be(
            "/v2/reporting/payment/details?paymentId=12345678");
        report.ProviderPaymentId.Should().Be(providerPaymentId);
        report.PaidPrice.Should().Be(110m);
        report.Items.Should().ContainSingle();
        report.Items[0].ProviderPaymentTransactionId.Should().Be("27225633");
        report.Items[0].Refunds.Should().ContainSingle(refund =>
            refund.ConversationId == "refund-conversation-001" && refund.Amount == 44m);
    }

    // Burada sayısal reporting paymentId değeri istenen provider kimliğinden saparsa cevabın reddedildiğini doğruluyorum.
    [Fact]
    public async Task RetrieveReversalReportAsync_Should_Reject_A_Different_Numeric_PaymentId()
    {
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            payments = new[]
            {
                new
                {
                    paymentId = 87654321,
                    paymentConversationId = "original-payment-conversation",
                    paymentRefundStatus = "NOT_REFUNDED",
                    currency = "TRY",
                    price = 100m,
                    paidPrice = 110m,
                    cancels = Array.Empty<object>(),
                    itemTransactions = Array.Empty<object>()
                }
            }
        }))));

        var action = () => gateway.RetrieveReversalReportAsync("12345678");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*payment identity does not match*");
    }

    // Burada imza yanıt için doğru olsa bile istenen conversation kimliğinden sapmanın reddedildiğini doğruluyorum.
    [Fact]
    public async Task RetrieveAsync_Should_Reject_Response_Bound_To_Different_Conversation()
    {
        const string requestedConversationId = "conversation-requested";
        const string responseConversationId = "conversation-attacker";
        const string token = "checkout-token-identity-001";
        var signature = SignResponse(
            "FAILURE", "failed-payment-002", "TRY", "basket-identity-001",
            responseConversationId, "100", "100", token);
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            conversationId = responseConversationId,
            price = 100m,
            paidPrice = 100m,
            currency = "TRY",
            basketId = "basket-identity-001",
            paymentId = "failed-payment-002",
            paymentStatus = "FAILURE",
            fraudStatus = -1,
            installment = 1,
            token,
            signature
        }))));

        var action = () => gateway.RetrieveAsync(token, requestedConversationId);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*integrity validation failed*");
    }

    // Burada rezervasyon mutabakatının taksit farkını kabul edip gerçek tahsilat ayrıntılarını döndürdüğünü doğruluyorum.
    [Fact]
    public async Task ReconcilePendingPaymentAsync_Should_Accept_Installment_Surcharge()
    {
        var orderId = Guid.Parse("8cdb1408-16e6-47ca-99da-2d3de248eeab");
        var paymentId = Guid.Parse("7c0bcdad-f97f-43cc-a633-e9a7e7fd4197");
        const string token = "checkout-token-installment";
        const string providerPaymentId = "12345679";
        const decimal basketPrice = 100m;
        const decimal orderAmount = 125.50m;
        const decimal providerPaidAmount = 139.25m;
        var signature = SignResponse(
            "SUCCESS",
            providerPaymentId,
            "TRY",
            orderId.ToString("N"),
            paymentId.ToString("N"),
            "139.25",
            "100",
            token);
        var gateway = CreateGateway(new StubHttpMessageHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, new
        {
            status = "success",
            conversationId = paymentId.ToString("N"),
            price = basketPrice,
            paidPrice = providerPaidAmount,
            currency = "TRY",
            basketId = orderId.ToString("N"),
            paymentId = providerPaymentId,
            paymentStatus = "SUCCESS",
            fraudStatus = 1,
            installment = 6,
            token,
            signature
        }))));

        var result = await gateway.ReconcilePendingPaymentAsync(
            new PaymentReconciliationRequest(
                orderId,
                paymentId,
                basketPrice,
                orderAmount,
                "IDEMPOTENCY_KEY",
                token));

        result.Status.Should().Be(PaymentReconciliationStatus.Paid);
        result.TransactionId.Should().Be(providerPaymentId);
        result.ProviderPaidAmount.Should().Be(providerPaidAmount);
        result.InstallmentCount.Should().Be(6);
        result.FraudStatus.Should().Be(1);
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
