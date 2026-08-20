using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Globalization;
using ECommerce.Application.Common.Interfaces;
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
    private const string OrderStatusChangedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.OrderStatusChangedEmailTemplate.html";
    private const string ReturnRequestedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.ReturnRequestedEmailTemplate.html";
    private const string ReturnStatusChangedTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.ReturnStatusChangedEmailTemplate.html";
    private const string GuestOrderAccessTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.GuestOrderAccessEmailTemplate.html";

    private readonly IConfiguration _configuration;
    private readonly IStoreSettingsRepository _storeSettingsRepository;

    // Burada SMTP e-posta göndericisini uygulama ayarlarıyla hazırlıyorum.
    public SmtpEmailSender(IConfiguration configuration, IStoreSettingsRepository storeSettingsRepository)
    {
        _configuration = configuration;
        _storeSettingsRepository = storeSettingsRepository;
    }

    private async Task<(string StoreName, string LogoHtml)> GetStoreContextAsync(CancellationToken cancellationToken)
    {
        var storeSettings = await _storeSettingsRepository.GetAsync(false, cancellationToken);
        var storeName = storeSettings?.DisplayName ?? "Mağaza";

        var logoHtml = string.IsNullOrWhiteSpace(storeSettings?.LogoUrl)
            ? $"<span style=\"color: #ffffff; font-size: 24px; font-weight: bold; text-decoration: none;\">{HtmlEncoder.Default.Encode(storeName)}</span>"
            : $"<img src=\"{HtmlEncoder.Default.Encode(storeSettings.LogoUrl)}\" alt=\"{HtmlEncoder.Default.Encode(storeName)}\" style=\"max-height: 60px; display: block; border: 0; margin: 0 auto;\" />";

        return (storeName, logoHtml);
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
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(PasswordResetTemplateResource)
            .Replace("{{PasswordResetLink}}", HtmlEncoder.Default.Encode(passwordResetLink), StringComparison.Ordinal)
            .Replace("{{ExpiresAt}}", HtmlEncoder.Default.Encode(expiresAt.ToString("g", CultureInfo.GetCultureInfo("tr-TR"))), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

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
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(WelcomeTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{WelcomeUrl}}", HtmlEncoder.Default.Encode(welcomeUrl), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

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
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(OrderCreatedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Amount}}", HtmlEncoder.Default.Encode(FormatAmount(grandTotal)), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

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
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(PaymentPaidTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Amount}}", HtmlEncoder.Default.Encode(FormatAmount(amount)), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

        await SendAsync(email, "Ödemeniz alındı", body, cancellationToken);
    }

    // Burada başarısız ödeme template'ini güvenilir ödeme snapshot'ıyla doldurup gönderiyorum.
    public async Task SendPaymentFailedAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(PaymentFailedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Amount}}", HtmlEncoder.Default.Encode(FormatAmount(amount)), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

        await SendAsync(email, "Ödeme işlemi tamamlanamadı", body, cancellationToken);
    }

    // Burada sipariş durum değişikliği template'ini güvenilir durum snapshot'ıyla doldurup gönderiyorum.
    public async Task SendOrderStatusChangedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string status,
        CancellationToken cancellationToken = default)
    {
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(OrderStatusChangedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{Status}}", HtmlEncoder.Default.Encode(status), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

        await SendAsync(email, "Siparişinizin durumu güncellendi", body, cancellationToken);
    }

    // Burada iade talebi template'ini güvenilir iade snapshot'ıyla doldurup gönderiyorum.
    public async Task SendReturnRequestedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string returnNumber,
        CancellationToken cancellationToken = default)
    {
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(ReturnRequestedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{ReturnNumber}}", HtmlEncoder.Default.Encode(returnNumber), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

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
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(ReturnStatusChangedTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{ReturnNumber}}", HtmlEncoder.Default.Encode(returnNumber), StringComparison.Ordinal)
            .Replace("{{Status}}", HtmlEncoder.Default.Encode(status), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

        await SendAsync(email, "İade talebinizin durumu güncellendi", body, cancellationToken);
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
        var (storeName, logoHtml) = await GetStoreContextAsync(cancellationToken);

        var body = LoadTemplate(GuestOrderAccessTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{OrderNumber}}", HtmlEncoder.Default.Encode(orderNumber), StringComparison.Ordinal)
            .Replace("{{AccessLink}}", HtmlEncoder.Default.Encode(link), StringComparison.Ordinal)
            .Replace("{{ExpiresAt}}", HtmlEncoder.Default.Encode(expiresAt.ToString("g", CultureInfo.GetCultureInfo("tr-TR"))), StringComparison.Ordinal)
            .Replace("{{StoreName}}", HtmlEncoder.Default.Encode(storeName), StringComparison.Ordinal)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.Ordinal);

        await SendAsync(email, "Siparişinize güvenli erişim bağlantısı", body, cancellationToken);
    }

    private async Task SendAsync(
        string email,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
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
