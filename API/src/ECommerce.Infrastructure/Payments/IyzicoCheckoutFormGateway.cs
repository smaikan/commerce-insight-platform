using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Common.Payments;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Payments;

public sealed class IyzicoCheckoutFormGateway : ICheckoutFormGateway, IPaymentGatewayReconciler
{
    private const string InitializePath = "/payment/iyzipos/checkoutform/initialize/auth/ecom";
    private const string RetrievePath = "/payment/iyzipos/checkoutform/auth/ecom/detail";
    private const string CancelPath = "/payment/cancel";
    private const string RefundPath = "/payment/refund";
    private const string ReportingDetailsPath = "/v2/reporting/payment/details";
    private const string SandboxPaymentPageHost = "sandbox-cpp.iyzipay.com";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly HttpClient _httpClient;
    private readonly IyzicoOptions _options;
    private readonly ILogger<IyzicoCheckoutFormGateway> _logger;

    public PaymentProvider Provider => PaymentProvider.Iyzico;
    public bool IsEnabled => _options.Enabled;

    // Burada test edilebilir HTTP istemcisi ve doğrulanmış sandbox ayarlarını hazırlıyorum.
    public IyzicoCheckoutFormGateway(
        HttpClient httpClient,
        IOptions<IyzicoOptions> options,
        ILogger<IyzicoCheckoutFormGateway> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    // Burada kart verisi almadan iyzico hosted CheckoutForm oturumunu oluşturuyorum.
    public async Task<CheckoutFormInitializeResult> InitializeAsync(
        CheckoutFormInitializeGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var payload = new
        {
            locale = "tr",
            request.ConversationId,
            request.Price,
            request.PaidPrice,
            currency = "TRY",
            request.BasketId,
            paymentGroup = "PRODUCT",
            callbackUrl = _options.CallbackUrl,
            enabledInstallments = _options.EnabledInstallments.Distinct().OrderBy(value => value).ToArray(),
            buyer = new
            {
                request.Buyer.Id,
                name = request.Buyer.FirstName,
                surname = request.Buyer.LastName,
                identityNumber = _options.SandboxBuyerIdentityNumber,
                request.Buyer.Email,
                gsmNumber = request.Buyer.PhoneNumber,
                registrationAddress = request.BillingAddress.FullAddress,
                ip = request.ClientIpAddress,
                request.BillingAddress.City,
                country = _options.Country,
                zipCode = request.BillingAddress.PostalCode
            },
            shippingAddress = MapAddress(request.ShippingAddress),
            billingAddress = MapAddress(request.BillingAddress),
            basketItems = request.Items.Select(item => new
            {
                item.Id,
                item.Price,
                item.Name,
                category1 = "ECommerce",
                category2 = "Product",
                itemType = "PHYSICAL"
            }).ToArray()
        };
        var response = await SendAsync<InitializeResponse>(InitializePath, payload, cancellationToken);
        var hasToken = !string.IsNullOrWhiteSpace(response.Token);
        var conversationMatches = string.Equals(
            response.ConversationId,
            request.ConversationId,
            StringComparison.Ordinal);
        var hasSignature = !string.IsNullOrWhiteSpace(response.Signature);
        var signatureValid = hasSignature && ValidateResponseSignature(
            response.Signature!,
            response.ConversationId,
            response.Token);
        if (!string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            if (!hasToken || !conversationMatches || !signatureValid)
            {
                throw new InvalidOperationException(
                    "iyzico initialize failure response integrity validation failed.");
            }

            // Burada provider reddini credential, token veya payload yazmadan yalnız güvenli hata koduyla gözlemlenebilir kılıyorum.
            _logger.LogWarning(
                "iyzico CheckoutForm initialization was rejected. ErrorCode={ErrorCode}",
                response.ErrorCode ?? "unknown");
            return new CheckoutFormInitializeResult(
                false,
                response.Token,
                null,
                null,
                "iyzico rejected the payment form initialization.",
                true,
                response.ConversationId);
        }

        var hasPaymentPageUrl = !string.IsNullOrWhiteSpace(response.PaymentPageUrl);
        var paymentPageUrlTrusted = hasPaymentPageUrl && IsTrustedPaymentPageUrl(response.PaymentPageUrl!);
        if (!hasToken || !conversationMatches || !hasPaymentPageUrl || !hasSignature ||
            !signatureValid || !paymentPageUrlTrusted)
        {
            // Burada başarılı görünen fakat bütünlüğü geçmeyen yanıtta hassas değerleri değil yalnız kontrol bayraklarını logluyorum.
            _logger.LogWarning(
                "iyzico CheckoutForm response integrity failed. HasToken={HasToken}, HasPaymentPageUrl={HasPaymentPageUrl}, HasSignature={HasSignature}, SignatureValid={SignatureValid}, PaymentPageUrlTrusted={PaymentPageUrlTrusted}",
                hasToken,
                hasPaymentPageUrl,
                hasSignature,
                signatureValid,
                paymentPageUrlTrusted);
            throw new InvalidOperationException("iyzico initialize response integrity validation failed.");
        }

        return new CheckoutFormInitializeResult(
            true,
            response.Token,
            response.PaymentPageUrl,
            ResolveExpiration(response.TokenExpireTime),
            null,
            false,
            response.ConversationId);
    }

    // Burada callback tokenıyla iyzico'daki kesin ödeme sonucunu ve yanıt imzasını doğruluyorum.
    public async Task<CheckoutFormRetrieveResult> RetrieveAsync(
        string token,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var response = await SendAsync<RetrieveResponse>(
            RetrievePath,
            new { locale = "tr", conversationId, token },
            cancellationToken);

        var isApiSuccess = string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase);
        var isPaymentSuccess = string.Equals(response.PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);

        if (!isApiSuccess)
        {
            var tokenConflicts = !string.IsNullOrWhiteSpace(response.Token) &&
                !string.Equals(response.Token, token, StringComparison.Ordinal);
            var conversationConflicts = !string.IsNullOrWhiteSpace(response.ConversationId) &&
                !string.Equals(response.ConversationId, conversationId, StringComparison.Ordinal);
            if (tokenConflicts || conversationConflicts)
            {
                throw new InvalidOperationException("iyzico retrieve failure response identity validation failed.");
            }

            // Burada süresi dolmuş token gibi API retlerini kesin ödeme sonucu saymadan güvenli hata koduyla ayırıyorum.
            _logger.LogInformation(
                "iyzico CheckoutForm retrieve request was rejected. ErrorCode={ErrorCode}",
                response.ErrorCode ?? "unknown");
            throw new CheckoutFormProviderUnavailableException(
                "iyzico did not return an authoritative payment result.",
                response.ErrorCode);
        }

        if (isPaymentSuccess &&
            (!response.Installment.HasValue || !_options.EnabledInstallments.Contains(response.Installment.Value)))
        {
            throw new InvalidOperationException("iyzico retrieve response contains an unexpected installment count.");
        }

        var tokenMatches = string.Equals(response.Token, token, StringComparison.Ordinal);
        var conversationMatches = string.Equals(
            response.ConversationId,
            conversationId,
            StringComparison.Ordinal);
        var signatureValid = !string.IsNullOrWhiteSpace(response.Signature) &&
            ValidateResponseSignature(
                response.Signature,
                response.PaymentStatus,
                response.PaymentId,
                response.Currency,
                response.BasketId,
                response.ConversationId,
                FormatMoney(response.PaidPrice),
                FormatMoney(response.Price),
                response.Token);
        if (!tokenMatches || !conversationMatches || !signatureValid)
        {
            throw new InvalidOperationException("iyzico retrieve response integrity validation failed.");
        }

        var state = ResolveState(response);
        return new CheckoutFormRetrieveResult(
            state,
            response.Token ?? token,
            response.ConversationId ?? string.Empty,
            response.BasketId ?? string.Empty,
            response.Currency ?? string.Empty,
            response.Price,
            response.PaidPrice,
            response.Installment,
            response.PaymentId,
            response.FraudStatus,
            state == CheckoutFormPaymentState.Failed
                ? "iyzico rejected the payment attempt."
                : null,
            (response.ItemTransactions ?? [])
                .Select(item => new CheckoutFormItemTransaction(
                    item.PaymentTransactionId ?? string.Empty,
                    item.ItemId ?? string.Empty,
                    item.Price,
                    item.PaidPrice,
                    item.TransactionStatus))
                .ToList());
    }

    // Burada terk edilmiş siparişe geç ulaşan tahsilatı iyzico paymentId üzerinden aynı gün iptal ediyorum.
    public async Task<LatePaymentReversalResult> ReverseLatePaymentAsync(
        string providerPaymentId,
        string conversationId,
        decimal expectedAmount,
        CancellationToken cancellationToken = default)
    {
        var result = await CancelPaymentAsync(
            providerPaymentId,
            conversationId,
            expectedAmount,
            cancellationToken);
        return new LatePaymentReversalResult(result.Succeeded, result.Retryable, result.FailureReason);
    }

    // Burada reporting servisindeki güncel payment/cancel/refund durumunu kimlik ve tutar alanlarıyla normalize ediyorum.
    public async Task<PaymentReversalReport> RetrieveReversalReportAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var query = $"?paymentId={Uri.EscapeDataString(providerPaymentId)}";
        var response = await SendGetAsync<ReportingResponse>(ReportingDetailsPath, query, cancellationToken);
        if (!string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase) ||
            response.Payments is null || response.Payments.Count != 1)
        {
            throw new InvalidOperationException("iyzico reporting response integrity validation failed.");
        }

        var payment = response.Payments[0];
        var normalizedPaymentId = ReadProviderIdentifier(payment.PaymentId);
        if (string.IsNullOrWhiteSpace(normalizedPaymentId) ||
            !string.Equals(normalizedPaymentId, providerPaymentId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("iyzico reporting payment identity does not match.");
        }

        return new PaymentReversalReport(
            normalizedPaymentId,
            payment.PaymentConversationId ?? string.Empty,
            payment.Currency ?? string.Empty,
            payment.Price,
            payment.PaidPrice,
            payment.PaymentRefundStatus ?? "UNKNOWN",
            (payment.Cancels ?? []).Select(cancel => new PaymentReversalReportCancel(
                cancel.CancelConversationId ?? string.Empty,
                cancel.RefundPrice,
                cancel.RefundStatus,
                cancel.CurrencyCode ?? string.Empty)).ToList(),
            (payment.ItemTransactions ?? []).Select(item => new PaymentReversalReportItem(
                ReadProviderIdentifier(item.PaymentTransactionId) ?? string.Empty,
                item.Price,
                item.PaidPrice,
                (item.Refunds ?? []).Select(refund => new PaymentReversalReportRefund(
                    refund.RefundConversationId ?? string.Empty,
                    refund.RefundPrice,
                    refund.RefundStatus,
                    refund.CurrencyCode ?? string.Empty)).ToList())).ToList());
    }

    // Burada aynı gün tam cancel cevabının payment, conversation, tutar ve para birimini eksiksiz doğruluyorum.
    public async Task<PaymentReversalGatewayResult> CancelPaymentAsync(
        string providerPaymentId,
        string conversationId,
        decimal expectedPaidAmount,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var response = await SendAsync<CancelResponse>(
            CancelPath,
            new { locale = "tr", conversationId, paymentId = providerPaymentId },
            cancellationToken);
        if (!string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("iyzico cancellation was rejected. ErrorCode={ErrorCode}", response.ErrorCode ?? "unknown");
            var identityBound = string.Equals(response.ConversationId, conversationId, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(response.PaymentId) &&
                string.Equals(response.PaymentId, providerPaymentId, StringComparison.Ordinal) &&
                (response.Price == 0m || response.Price == expectedPaidAmount);
            return new PaymentReversalGatewayResult(
                false,
                response.Retryable == true || !identityBound,
                response.ErrorCode,
                "iyzico rejected the payment cancellation.");
        }

        if (!string.Equals(response.ConversationId, conversationId, StringComparison.Ordinal) ||
            !string.Equals(response.PaymentId, providerPaymentId, StringComparison.Ordinal) ||
            response.Price != expectedPaidAmount ||
            !string.Equals(response.Currency, "TRY", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("iyzico cancellation response does not match the payment.");
        }

        return new PaymentReversalGatewayResult(true, false);
    }

    // Burada standart refund cevabını resmi HMAC alan sırası ve bütün finansal kimliklerle doğruluyorum.
    public async Task<PaymentReversalGatewayResult> RefundPaymentItemAsync(
        string providerPaymentId,
        string providerPaymentTransactionId,
        string conversationId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var response = await SendAsync<RefundResponse>(
            RefundPath,
            new
            {
                locale = "tr",
                conversationId,
                paymentTransactionId = providerPaymentTransactionId,
                price = amount,
                currency = "TRY"
            },
            cancellationToken);
        if (!string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("iyzico item refund was rejected. ErrorCode={ErrorCode}", response.ErrorCode ?? "unknown");
            var identityBound = string.Equals(response.ConversationId, conversationId, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(response.PaymentId) &&
                string.Equals(response.PaymentId, providerPaymentId, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(response.PaymentTransactionId) &&
                string.Equals(response.PaymentTransactionId, providerPaymentTransactionId, StringComparison.Ordinal) &&
                (response.Price == 0m || response.Price == amount);
            return new PaymentReversalGatewayResult(
                false,
                response.Retryable == true || !identityBound,
                response.ErrorCode,
                "iyzico rejected an item refund.");
        }

        var signatureValid = !string.IsNullOrWhiteSpace(response.Signature) && ValidateResponseSignature(
            response.Signature,
            response.PaymentId,
            FormatMoney(response.Price),
            response.Currency,
            response.ConversationId);
        if (!signatureValid ||
            !string.Equals(response.PaymentId, providerPaymentId, StringComparison.Ordinal) ||
            !string.Equals(response.PaymentTransactionId, providerPaymentTransactionId, StringComparison.Ordinal) ||
            !string.Equals(response.ConversationId, conversationId, StringComparison.Ordinal) ||
            response.Price != amount ||
            !string.Equals(response.Currency, "TRY", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("iyzico refund response integrity validation failed.");
        }

        return new PaymentReversalGatewayResult(true, false);
    }

    // Burada yalnız güncel HPP X-IYZ-SIGNATURE-V3 biçimini kabul ediyorum.
    public bool ValidateWebhookSignature(CheckoutFormWebhookNotification notification, string signature)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var content = string.Concat(
            _options.SecretKey,
            notification.EventType,
            notification.ProviderPaymentId,
            notification.Token,
            notification.PaymentConversationId,
            notification.Status);
        var expected = ComputeHexHmac(content);
        return FixedTimeEquals(expected, signature.Trim());
    }

    // Burada süresi dolan yerel rezervasyon için iyzico'daki kesin ödeme durumunu yeniden sorguluyorum.
    public async Task<PaymentReconciliationResult> ReconcilePendingPaymentAsync(
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderToken))
        {
            return new PaymentReconciliationResult(PaymentReconciliationStatus.Unknown);
        }

        var result = await RetrieveAsync(
            request.ProviderToken,
            request.PaymentId.ToString("N"),
            cancellationToken);
        if (!string.Equals(result.Token, request.ProviderToken, StringComparison.Ordinal) ||
            !string.Equals(result.ConversationId, request.PaymentId.ToString("N"), StringComparison.Ordinal))
        {
            return new PaymentReconciliationResult(PaymentReconciliationStatus.Unknown);
        }

        return result.State switch
        {
            CheckoutFormPaymentState.Paid when
                !string.IsNullOrWhiteSpace(result.ProviderPaymentId) &&
                result.Price == request.BasketPrice &&
                result.PaidPrice >= request.Amount &&
                string.Equals(result.Currency, "TRY", StringComparison.Ordinal) &&
                string.Equals(result.BasketId, request.OrderId.ToString("N"), StringComparison.Ordinal) &&
                result.InstallmentCount.HasValue &&
                result.InstallmentCount is >= 1 and <= 12 =>
                new PaymentReconciliationResult(
                    PaymentReconciliationStatus.Paid,
                    result.ProviderPaymentId,
                    result.PaidPrice,
                    result.InstallmentCount,
                    result.FraudStatus),
            CheckoutFormPaymentState.Failed =>
                new PaymentReconciliationResult(PaymentReconciliationStatus.Cancelled),
            _ => new PaymentReconciliationResult(PaymentReconciliationStatus.Unknown)
        };
    }

    // Burada iyzico adres şemasını güvenilir sipariş snapshot'ından oluşturuyorum.
    private object MapAddress(CheckoutFormAddress address)
    {
        return new
        {
            address = $"{address.FullAddress}, {address.District}",
            zipCode = address.PostalCode,
            address.ContactName,
            address.City,
            country = _options.Country
        };
    }

    // Burada aynı JSON gövdesini hem IYZWSv2 imzasında hem HTTP isteğinde kullanıyorum.
    private async Task<TResponse> SendAsync<TResponse>(
        string path,
        object payload,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(payload, JsonOptions);
        var randomKey = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{RandomNumberGenerator.GetHexString(8)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "IYZWSv2",
            CreateAuthorizationValue(path, body, randomKey));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"iyzico returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        return JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("iyzico returned an empty response.");
    }

    // Burada reporting GET isteğini body olmadan resmî path tabanlı IYZWSv2 imzasıyla gönderiyorum.
    private async Task<TResponse> SendGetAsync<TResponse>(
        string path,
        string query,
        CancellationToken cancellationToken)
    {
        var randomKey = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{RandomNumberGenerator.GetHexString(8)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(path, query));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "IYZWSv2",
            CreateAuthorizationValue(path, string.Empty, randomKey));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"iyzico returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        return JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("iyzico returned an empty reporting response.");
    }

    // Burada resmi IYZWSv2 HMACSHA256 yetkilendirme değerini üretiyorum.
    private string CreateAuthorizationValue(string path, string body, string randomKey)
    {
        var signature = ComputeHexHmac(string.Concat(randomKey, path, body));
        var authorizationPayload = $"apiKey:{_options.ApiKey}&randomKey:{randomKey}&signature:{signature}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationPayload));
    }

    // Burada resmi iki nokta ayraçlı yanıt imzasını sabit zamanlı karşılaştırıyorum.
    private bool ValidateResponseSignature(string providedSignature, params string?[] values)
    {
        var safeValues = values.Select(v => v ?? string.Empty).ToArray();
        return FixedTimeEquals(ComputeHexHmac(string.Join(':', safeValues)), providedSignature.Trim());
    }

    // Burada HMACSHA256 sonucunu iyzico'nun beklediği küçük harfli hex biçimine getiriyorum.
    private string ComputeHexHmac(string content)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }

    // Burada sandbox'ın sayı, production'ın metin döndürebildiği provider kimliğini tek kanonik metne indiriyorum.
    private static string? ReadProviderIdentifier(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    // Burada iki hex imzayı süre sızıntısı oluşturmadan karşılaştırıyorum.
    private static bool FixedTimeEquals(string expected, string provided)
    {
        if (expected.Length != provided.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(provided.ToLowerInvariant()));
    }

    // Burada para değerlerini iyzico imza sözleşmesindeki ardıl sıfırsız biçime getiriyorum.
    private static string FormatMoney(decimal value)
    {
        return value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    // Burada initialize yanıtındaki saniye veya epoch değerini güvenli UTC son kullanma zamanına çeviriyorum.
    private static DateTime ResolveExpiration(long? tokenExpireTime)
    {
        if (!tokenExpireTime.HasValue || tokenExpireTime.Value <= 0)
        {
            return DateTime.UtcNow.AddMinutes(30);
        }

        return tokenExpireTime.Value > 10_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(tokenExpireTime.Value).UtcDateTime
            : DateTime.UtcNow.AddSeconds(Math.Min(tokenExpireTime.Value, 86_400));
    }

    // Burada iyzico ödeme ve fraud durumlarını yerel üç durumlu sonuca indirgerim.
    private static CheckoutFormPaymentState ResolveState(RetrieveResponse response)
    {
        if (!string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return CheckoutFormPaymentState.Pending;
        }

        if (response.FraudStatus == 0)
        {
            return CheckoutFormPaymentState.Pending;
        }

        if (string.Equals(response.PaymentStatus, "FAILURE", StringComparison.OrdinalIgnoreCase))
        {
            return CheckoutFormPaymentState.Failed;
        }

        if (!string.Equals(response.PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return CheckoutFormPaymentState.Pending;
        }

        return response.FraudStatus switch
        {
            1 => CheckoutFormPaymentState.Paid,
            -1 => CheckoutFormPaymentState.Failed,
            _ => CheckoutFormPaymentState.Pending
        };
    }

    // Burada provider'ın yönlendirme URL'sini iyzico'nun resmi sandbox CheckoutForm origin'iyle sınırlandırıyorum.
    private bool IsTrustedPaymentPageUrl(string paymentPageUrl)
    {
        return Uri.TryCreate(paymentPageUrl, UriKind.Absolute, out var paymentUri) &&
               paymentUri.Scheme == Uri.UriSchemeHttps &&
               string.Equals(paymentUri.Host, SandboxPaymentPageHost, StringComparison.OrdinalIgnoreCase) &&
               paymentUri.IsDefaultPort;
    }

    // Burada kapalı provider'ın yanlışlıkla ağ çağrısı yapmasını engelliyorum.
    private void EnsureEnabled()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("iyzico CheckoutForm is not enabled.");
        }
    }

    private sealed class InitializeResponse
    {
        public string? Status { get; init; }
        public string? ErrorCode { get; init; }
        public string? ConversationId { get; init; }
        public string? Token { get; init; }
        public string? PaymentPageUrl { get; init; }
        public long? TokenExpireTime { get; init; }
        public string? Signature { get; init; }
    }

    private sealed class RetrieveResponse
    {
        public string? Status { get; init; }
        public string? ErrorCode { get; init; }
        public string? ConversationId { get; init; }
        public decimal Price { get; init; }
        public decimal PaidPrice { get; init; }
        public int? Installment { get; init; }
        public string? Currency { get; init; }
        public string? BasketId { get; init; }
        public string? PaymentId { get; init; }
        public string? PaymentStatus { get; init; }
        public int? FraudStatus { get; init; }
        public string? Token { get; init; }
        public string? Signature { get; init; }
        public List<RetrieveItemTransaction>? ItemTransactions { get; init; }
    }

    private sealed class RetrieveItemTransaction
    {
        public string? PaymentTransactionId { get; init; }
        public string? ItemId { get; init; }
        public decimal Price { get; init; }
        public decimal PaidPrice { get; init; }
        public int TransactionStatus { get; init; }
    }

    private sealed class CancelResponse
    {
        public string? Status { get; init; }
        public string? ErrorCode { get; init; }
        public string? ConversationId { get; init; }
        public string? PaymentId { get; init; }
        public decimal Price { get; init; }
        public string? Currency { get; init; }
        public bool? Retryable { get; init; }
    }

    private sealed class RefundResponse
    {
        public string? Status { get; init; }
        public string? ErrorCode { get; init; }
        public string? ConversationId { get; init; }
        public string? PaymentId { get; init; }
        public string? PaymentTransactionId { get; init; }
        public decimal Price { get; init; }
        public string? Currency { get; init; }
        public bool? Retryable { get; init; }
        public string? Signature { get; init; }
    }

    private sealed class ReportingResponse
    {
        public string? Status { get; init; }
        public List<ReportingPayment>? Payments { get; init; }
    }

    private sealed class ReportingPayment
    {
        public JsonElement PaymentId { get; init; }
        public string? PaymentConversationId { get; init; }
        public string? PaymentRefundStatus { get; init; }
        public string? Currency { get; init; }
        public decimal Price { get; init; }
        public decimal PaidPrice { get; init; }
        public List<ReportingCancel>? Cancels { get; init; }
        public List<ReportingItem>? ItemTransactions { get; init; }
    }

    private sealed class ReportingCancel
    {
        public string? CancelConversationId { get; init; }
        public decimal RefundPrice { get; init; }
        public int RefundStatus { get; init; }
        public string? CurrencyCode { get; init; }
    }

    private sealed class ReportingItem
    {
        public JsonElement PaymentTransactionId { get; init; }
        public decimal Price { get; init; }
        public decimal PaidPrice { get; init; }
        public List<ReportingRefund>? Refunds { get; init; }
    }

    private sealed class ReportingRefund
    {
        public string? RefundConversationId { get; init; }
        public decimal RefundPrice { get; init; }
        public int RefundStatus { get; init; }
        public string? CurrencyCode { get; init; }
    }
}
