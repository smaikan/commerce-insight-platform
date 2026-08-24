using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Globalization;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private const string PasswordResetTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.PasswordResetEmailTemplate.html";
    private const string WelcomeTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.WelcomeEmailTemplate.html";
    private const string OrderCreatedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.OrderCreatedEmailTemplate.html";
    private const string PaymentPaidTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.PaymentPaidEmailTemplate.html";
    private const string PaymentFailedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.PaymentFailedEmailTemplate.html";
    private const string PaymentReversalCompletedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.PaymentReversalCompletedEmailTemplate.html";
    private const string OrderStatusChangedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.OrderStatusChangedEmailTemplate.html";
    private const string ReturnRequestedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.ReturnRequestedEmailTemplate.html";
    private const string ReturnStatusChangedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.ReturnStatusChangedEmailTemplate.html";
    private const string GuestOrderAccessTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.GuestOrderAccessEmailTemplate.html";
    private const string ContactMessageReceivedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.ContactMessageReceivedEmailTemplate.html";
    private const string ContactMessageReplyTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.ContactMessageReplyEmailTemplate.html";

    private readonly IConfiguration _configuration;
    private readonly IStoreSettingsRepository _storeSettingsRepository;

    // Burada SMTP e-posta göndericisini uygulama ayarlarıyla hazırlıyorum.
    public SmtpEmailSender(IConfiguration configuration, IStoreSettingsRepository storeSettingsRepository)
    {
        _configuration = configuration;
        _storeSettingsRepository = storeSettingsRepository;
    }

    // Burada e-posta şablonlarında gösterilecek mağaza adını kalıcı ayarlardan çözümlüyorum.
    private async Task<string> GetStoreNameAsync(CancellationToken cancellationToken)
    {
        var storeSettings = await _storeSettingsRepository.GetAsync(false, cancellationToken);
        return string.IsNullOrWhiteSpace(storeSettings?.DisplayName)
            ? "ELEVEN"
            : storeSettings.DisplayName;
    }

    // Burada parola sıfırlama template'ini güvenli bağlantı verileriyle doldurup gönderiyorum.
    public async Task SendPasswordResetAsync(
        string email,
        string rawToken,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = GetRequiredValue("Email:PasswordResetUrl");
        var passwordResetLink = BuildPasswordResetLink(resetUrl, rawToken);
        var storeName = await GetStoreNameAsync(cancellationToken);

        var body = LoadTemplate(PasswordResetTemplateResource)
            .Replace("{{PasswordResetLink}}", HtmlEncoder.Default.Encode(passwordResetLink), StringComparison.Ordinal)
            .Replace("{{ExpiresAt}}", HtmlEncoder.Default.Encode(expiresAt.ToString("g", CultureInfo.GetCultureInfo("tr-TR"))), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "Parola sıfırlama bağlantınız", body, cancellationToken);
    }

    // Burada hassas reset tokenını proxy, sunucu logu ve referrer üzerinden taşınmayan URL fragment'ına yerleştiriyorum.
    internal static string BuildPasswordResetLink(string resetUrl, string rawToken)
    {
        var uriBuilder = new UriBuilder(resetUrl)
        {
            Fragment = $"token={Uri.EscapeDataString(rawToken)}"
        };
        return uriBuilder.Uri.AbsoluteUri;
    }

    // Burada hoş geldin template'ini güvenli kullanıcı bilgileriyle doldurup gönderiyorum.
    public async Task SendWelcomeAsync(
        string email,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        var welcomeUrl = GetRequiredValue("Email:WelcomeUrl");
        var storeName = await GetStoreNameAsync(cancellationToken);

        var body = LoadTemplate(WelcomeTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{WelcomeUrl}}", HtmlEncoder.Default.Encode(welcomeUrl), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "Aramıza hoş geldiniz", body, cancellationToken);
    }

    // Burada sipariş oluşturma template'ini güvenilir sipariş snapshot'ıyla doldurup gönderiyorum.
    public async Task SendOrderCreatedAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal grandTotal,
        CancellationToken cancellationToken = default)
    {
        var storeName = await GetStoreNameAsync(cancellationToken);

        var body = LoadTemplate(OrderCreatedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Amount}}", HtmlEncoder.Default.Encode(FormatAmount(grandTotal)), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "Siparişiniz alındı", body, cancellationToken);
    }

    // Burada başarılı ödeme template'ini güvenilir ödeme snapshot'ıyla doldurup gönderiyorum.
    public async Task SendPaymentPaidAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var storeName = await GetStoreNameAsync(cancellationToken);

        var body = LoadTemplate(PaymentPaidTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Amount}}", HtmlEncoder.Default.Encode(FormatAmount(amount)), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "Siparişiniz ve ödemeniz alındı", body, cancellationToken);
    }

    // Burada başarısız ödeme template'ini güvenilir ödeme snapshot'ıyla doldurup gönderiyorum.
    public async Task SendPaymentFailedAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var storeName = await GetStoreNameAsync(cancellationToken);

        var body = LoadTemplate(PaymentFailedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Amount}}", HtmlEncoder.Default.Encode(FormatAmount(amount)), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "Ödeme işlemi tamamlanamadı", body, cancellationToken);
    }

    // Burada doğrulanmış cancel veya refund sonucunu gerçek tutar ve güvenli müşteri metniyle gönderiyorum.
    public async Task SendPaymentReversalCompletedAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal amount,
        string reversalType,
        CancellationToken cancellationToken = default)
    {
        var storeName = await GetStoreNameAsync(cancellationToken);
        var description = FormatPaymentReversalDescription(reversalType);
        var body = LoadTemplate(PaymentReversalCompletedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Amount}}", HtmlEncoder.Default.Encode(FormatAmount(amount)), StringComparison.Ordinal)
            .Replace("{{ReversalDescription}}", HtmlEncoder.Default.Encode(description), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "Ücret iadeniz gerçekleştirildi", body, cancellationToken);
    }

    // Burada kargo ayrıntılarını yalnız kargoya verildi durumunda render ederek sipariş durum e-postasını gönderiyorum.
    public async Task SendOrderStatusChangedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string status,
        string? shippingCarrier = null,
        string? trackingNumber = null,
        string? trackingUrl = null,
        CancellationToken cancellationToken = default)
    {
        var storeName = await GetStoreNameAsync(cancellationToken);
        var localizedStatus = FormatOrderStatus(status);
        var includeShipmentDetails = Enum.TryParse<OrderStatus>(status, true, out var orderStatus) &&
            orderStatus == OrderStatus.Shipped;
        var shipmentHtml = includeShipmentDetails
            ? BuildShipmentHtml(shippingCarrier, trackingNumber, trackingUrl)
            : string.Empty;

        var body = LoadTemplate(OrderStatusChangedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Status}}", HtmlEncoder.Default.Encode(localizedStatus), StringComparison.Ordinal)
            .Replace("{{ShipmentHtml}}", shipmentHtml, StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, $"Siparişinizin durumu güncellendi: {localizedStatus}", body, cancellationToken);
    }

    // Burada yalnız güvenli ve encode edilmiş kargo alanlarından opsiyonel takip bloğu oluşturuyorum.
    private static string BuildShipmentHtml(string? shippingCarrier, string? trackingNumber, string? trackingUrl)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber) && string.IsNullOrWhiteSpace(shippingCarrier))
        {
            return string.Empty;
        }

        var carrierText = !string.IsNullOrWhiteSpace(shippingCarrier)
            ? HtmlEncoder.Default.Encode(shippingCarrier.Trim())
            : "Kargo";

        var trackingNumberHtml = !string.IsNullOrWhiteSpace(trackingNumber)
            ? $"<p style=\"margin: 0 0 6px 0; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 600;\">Takip Numarası</p><p style=\"margin: 0 0 15px 0; font-size: 18px; font-weight: bold; color: #0b0f19; font-family: monospace; letter-spacing: 1px;\">{HtmlEncoder.Default.Encode(trackingNumber.Trim())}</p>"
            : string.Empty;

        var trackingButtonHtml = !string.IsNullOrWhiteSpace(trackingUrl)
            ? $"<div style=\"margin-top: 15px;\"><a href=\"{HtmlEncoder.Default.Encode(trackingUrl.Trim())}\" target=\"_blank\" style=\"background-color: #0b0f19; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-size: 14px; font-weight: bold; display: inline-block;\">Kargomu Takip Et &rarr;</a></div>"
            : string.Empty;

        return $@"
        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f8faff; border: 1px solid #d1e0ff; border-radius: 8px; margin-top: 20px; margin-bottom: 25px;"">
            <tr>
                <td style=""padding: 20px 25px;"">
                    <p style=""margin: 0 0 6px 0; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 600;"">Kargo Firması</p>
                    <p style=""margin: 0 0 15px 0; font-size: 16px; font-weight: bold; color: #0b0f19;"">{carrierText}</p>
                    {trackingNumberHtml}
                    {trackingButtonHtml}
                </td>
            </tr>
        </table>";
    }

    // Burada iade talebi template'ini güvenilir iade snapshot'ıyla doldurup gönderiyorum.
    public async Task SendReturnRequestedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string returnNumber,
        CancellationToken cancellationToken = default)
    {
        var storeName = await GetStoreNameAsync(cancellationToken);

        var body = LoadTemplate(ReturnRequestedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{ReturnNumber}}", HtmlEncoder.Default.Encode(returnNumber), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "İade talebiniz alındı", body, cancellationToken);
    }

    // Burada iade durum değişikliği template'ini güvenilir iade snapshot'ıyla doldurup gönderiyorum.
    public async Task SendReturnStatusChangedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string returnNumber,
        string status,
        CancellationToken cancellationToken = default)
    {
        var storeName = await GetStoreNameAsync(cancellationToken);
        var localizedStatus = FormatReturnStatus(status);

        var body = LoadTemplate(ReturnStatusChangedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{ReturnNumber}}", HtmlEncoder.Default.Encode(returnNumber), StringComparison.Ordinal)
            .Replace("{{Status}}", HtmlEncoder.Default.Encode(localizedStatus), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, $"İade talebinizin durumu güncellendi: {localizedStatus}", body, cancellationToken);
    }

    // Burada sipariş durumunu müşteriye gösterilecek Türkçe metne dönüştürüyorum.
    private static string FormatOrderStatus(string status)
    {
        if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            return orderStatus switch
            {
                OrderStatus.Pending => "Ödeme Bekleniyor",
                OrderStatus.Confirmed => "Sipariş Onaylandı",
                OrderStatus.Paid => "Ödeme Alındı",
                OrderStatus.Preparing => "Sipariş Hazırlanıyor",
                OrderStatus.Shipped => "Kargoya Verildi",
                OrderStatus.Delivered => "Teslim Edildi",
                OrderStatus.Cancelled => "Sipariş İptal Edildi",
                OrderStatus.Refunded => "Ücret İade Edildi",
                OrderStatus.ReturnRequested => "İade Talebi Oluşturuldu",
                OrderStatus.ReturnApproved => "İade Onaylandı",
                _ => status
            };
        }

        return status?.Trim().ToLowerInvariant() switch
        {
            "pending" => "Ödeme Bekleniyor",
            "confirmed" => "Sipariş Onaylandı",
            "paid" => "Ödeme Alındı",
            "preparing" => "Sipariş Hazırlanıyor",
            "shipped" => "Kargoya Verildi",
            "delivered" => "Teslim Edildi",
            "cancelled" or "canceled" => "Sipariş İptal Edildi",
            "refunded" => "Ücret İade Edildi",
            "returnrequested" => "İade Talebi Oluşturuldu",
            "returnapproved" => "İade Onaylandı",
            _ => status ?? string.Empty
        };
    }

    // Burada provider ters işlem türünü müşterinin anlayacağı güvenli açıklamaya dönüştürüyorum.
    private static string FormatPaymentReversalDescription(string reversalType)
    {
        if (!Enum.TryParse<PaymentReversalType>(reversalType, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new InvalidOperationException("Payment reversal type is invalid.");
        }

        return parsed == PaymentReversalType.Cancel
            ? "Ödemeniz ödeme sağlayıcısı tarafından iptal edildi."
            : "Ücret iadeniz ödeme sağlayıcısı tarafından onaylandı.";
    }

    // Burada iade talebi durumunu müşteriye gösterilecek Türkçe metne dönüştürüyorum.
    private static string FormatReturnStatus(string status)
    {
        if (Enum.TryParse<ReturnRequestStatus>(status, true, out var returnStatus))
        {
            return returnStatus switch
            {
                ReturnRequestStatus.Requested => "Talep Alındı",
                ReturnRequestStatus.Approved => "İade Onaylandı",
                ReturnRequestStatus.Rejected => "İade Reddedildi",
                ReturnRequestStatus.Received => "Ürün Teslim Alındı",
                ReturnRequestStatus.Completed => "İade Tamamlandı",
                _ => status
            };
        }

        return status?.Trim().ToLowerInvariant() switch
        {
            "requested" => "Talep Alındı",
            "approved" => "İade Onaylandı",
            "rejected" => "İade Reddedildi",
            "received" => "Ürün Teslim Alındı",
            "completed" => "İade Tamamlandı",
            _ => status ?? string.Empty
        };
    }

    // Burada hazırlanmış e-posta içeriğini kısa SMTP retry politikasıyla iletiyorum.
    // Burada guest magic-link tokenını URL fragment'ına koyarak referrer ve sunucu loglarından uzak tutuyorum.
    public async Task SendGuestOrderAccessAsync(
        string email,
        string recipientName,
        string orderNumber,
        string rawToken,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var accessUrl = GetRequiredValue("Email:GuestOrderAccessUrl");
        var link = $"{accessUrl}#token={Uri.EscapeDataString(rawToken)}";
        var storeName = await GetStoreNameAsync(cancellationToken);

        var body = LoadTemplate(GuestOrderAccessTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{AccessLink}}", HtmlEncoder.Default.Encode(link), StringComparison.Ordinal)
            .Replace("{{ExpiresAt}}", HtmlEncoder.Default.Encode(expiresAt.ToString("g", CultureInfo.GetCultureInfo("tr-TR"))), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal);

        await SendAsync(email, "Siparişinize güvenli erişim bağlantısı", body, cancellationToken);
    }

    // Burada contact başvurusundaki tüm kullanıcı alanlarını encode edip operasyonel inbox'a sabit başlıkla gönderiyorum.
    public async Task SendContactMessageReceivedAsync(
        string inboxEmail,
        string referenceNumber,
        string name,
        string customerEmail,
        string? phone,
        string subject,
        string? providedOrderNumber,
        string body,
        string? adminDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var template = LoadTemplate(ContactMessageReceivedTemplateResource)
            .Replace("{{ReferenceNumber}}", HtmlEncoder.Default.Encode(referenceNumber), StringComparison.Ordinal)
            .Replace("{{Name}}", HtmlEncoder.Default.Encode(name), StringComparison.Ordinal)
            .Replace("{{Email}}", HtmlEncoder.Default.Encode(customerEmail), StringComparison.Ordinal)
            .Replace("{{Phone}}", HtmlEncoder.Default.Encode(phone ?? "Belirtilmedi"), StringComparison.Ordinal)
            .Replace("{{Subject}}", HtmlEncoder.Default.Encode(subject), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(providedOrderNumber ?? "Belirtilmedi"), StringComparison.Ordinal)
            .Replace("{{Message}}", HtmlEncoder.Default.Encode(body).Replace("\n", "<br />", StringComparison.Ordinal), StringComparison.Ordinal)
            .Replace("{{AdminDetailUrl}}", HtmlEncoder.Default.Encode(adminDetailUrl ?? string.Empty), StringComparison.Ordinal);
        await SendAsync(inboxEmail, $"Yeni iletişim mesajı: {referenceNumber}", template, cancellationToken);
    }

    // Burada müşteri yanıtını kayıtlı alıcıya encode edilmiş body ve yapılandırılmış destek Reply-To ile gönderiyorum.
    public async Task SendContactMessageReplyAsync(
        string recipientEmail,
        string recipientName,
        string referenceNumber,
        string body,
        CancellationToken cancellationToken = default)
    {
        var template = LoadTemplate(ContactMessageReplyTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{ReferenceNumber}}", HtmlEncoder.Default.Encode(referenceNumber), StringComparison.Ordinal)
            .Replace("{{Message}}", HtmlEncoder.Default.Encode(body).Replace("\n", "<br />", StringComparison.Ordinal), StringComparison.Ordinal);
        await SendAsync(
            recipientEmail,
            $"İletişim talebiniz yanıtlandı: {referenceNumber}",
            template,
            cancellationToken,
            GetRequiredValue("Email:SupportReplyToAddress"));
    }

    private async Task SendAsync(
        string email,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken,
        string? replyToAddress = null)
    {
        var host = GetRequiredValue("Email:Smtp:Host");
        var fromAddress = GetRequiredValue("Email:FromAddress");
        var port = GetPositiveInt("Email:Smtp:Port", 587);
        var useSsl = GetBool("Email:Smtp:UseSsl", true);
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var fromName = _configuration["Email:FromName"] ?? "ECommerce";

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(email);
        if (!string.IsNullOrWhiteSpace(replyToAddress))
        {
            message.ReplyToList.Add(new MailAddress(replyToAddress));
        }

        using var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(username)
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            smtpClient.Credentials = new NetworkCredential(username, password);
        }

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await smtpClient.SendMailAsync(message, cancellationToken);
                return;
            }
            catch (SmtpException) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }
    }

    // Burada para tutarını müşteriye gösterilecek Türkçe sayı biçimine dönüştürüyorum.
    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
    }

    // Burada assembly içine gömülen güvenilir HTML template'ini okuyorum.
    private static string LoadTemplate(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Email template resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // Burada zorunlu e-posta ayarını okuyup eksikse işlemi güvenli biçimde durduruyorum.
    private string GetRequiredValue(string key)
    {
        return _configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{key} must be configured.");
    }

    // Burada pozitif sayı olarak beklenen SMTP ayarını varsayılan değerle okuyorum.
    private int GetPositiveInt(string key, int fallback)
    {
        return int.TryParse(_configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }

    // Burada boolean SMTP ayarını geçerli değilse varsayılan değerle okuyorum.
    private bool GetBool(string key, bool fallback)
    {
        return bool.TryParse(_configuration[key], out var value)
            ? value
            : fallback;
    }
}
