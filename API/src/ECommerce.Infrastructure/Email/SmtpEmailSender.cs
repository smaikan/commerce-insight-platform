using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text.Encodings.Web;
using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private const string PasswordResetTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.PasswordResetEmailTemplate.html";
    private const string WelcomeTemplateResource =
        "ECommerce.Infrastructure.Email.Templates.WelcomeEmailTemplate.html";

    private readonly IConfiguration _configuration;

    // Burada SMTP e-posta göndericisini uygulama ayarlarıyla hazırlıyorum.
    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Burada parola sıfırlama template'ini güvenli bağlantı verileriyle doldurup gönderiyorum.
    public async Task SendPasswordResetAsync(
        string email,
        string rawToken,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = GetRequiredValue("Email:PasswordResetUrl");
        var separator = resetUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var passwordResetLink = $"{resetUrl}{separator}token={Uri.EscapeDataString(rawToken)}";
        var body = LoadTemplate(PasswordResetTemplateResource)
            .Replace("{{PasswordResetLink}}", HtmlEncoder.Default.Encode(passwordResetLink), StringComparison.Ordinal)
            .Replace("{{ExpiresAt}}", HtmlEncoder.Default.Encode(expiresAt.ToString("O")), StringComparison.Ordinal);

        await SendAsync(email, "Parola sıfırlama bağlantınız", body, cancellationToken);
    }

    // Burada hoş geldin template'ini güvenli kullanıcı bilgileriyle doldurup gönderiyorum.
    public async Task SendWelcomeAsync(
        string email,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        var welcomeUrl = GetRequiredValue("Email:WelcomeUrl");
        var body = LoadTemplate(WelcomeTemplateResource)
            .Replace("{{RecipientName}}", HtmlEncoder.Default.Encode(recipientName), StringComparison.Ordinal)
            .Replace("{{WelcomeUrl}}", HtmlEncoder.Default.Encode(welcomeUrl), StringComparison.Ordinal);

        await SendAsync(email, "Aramıza hoş geldiniz", body, cancellationToken);
    }

    // Burada hazırlanmış e-posta içeriğini kısa SMTP retry politikasıyla iletiyorum.
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
