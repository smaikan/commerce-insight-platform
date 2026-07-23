using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class EmailOutboxMessage : BaseEntity
{
    public EmailOutboxMessageType Type { get; private set; }
    public string Email { get; private set; } = null!;
    public string? RecipientName { get; private set; }
    public string? ProtectedToken { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? LastError { get; private set; }

    // Burada EF Core'un kayıt yüklerken kullanacağı boş nesneyi oluşturuyorum.
    private EmailOutboxMessage()
    {
    }

    // Burada parola sıfırlama e-postasını güvenli token bilgisiyle kuyruğa hazırlıyorum.
    public static EmailOutboxMessage CreatePasswordReset(
        string email,
        string protectedToken,
        DateTime expiresAt,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(protectedToken))
        {
            throw new DomainException("Protected email token is required.");
        }

        if (expiresAt <= createdAt)
        {
            throw new DomainException("Token expiry date must be in the future.");
        }

        return new EmailOutboxMessage
        {
            Type = EmailOutboxMessageType.PasswordReset,
            Email = NormalizeEmail(email),
            ProtectedToken = protectedToken,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            NextAttemptAt = createdAt
        };
    }

    // Burada yeni kayıt için gönderilecek hoş geldin e-postasını kuyruğa hazırlıyorum.
    public static EmailOutboxMessage CreateWelcome(string email, string recipientName, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new DomainException("Email recipient name is required.");
        }

        return new EmailOutboxMessage
        {
            Type = EmailOutboxMessageType.Welcome,
            Email = NormalizeEmail(email),
            RecipientName = recipientName.Trim(),
            CreatedAt = createdAt,
            NextAttemptAt = createdAt
        };
    }

    // Burada süresi olan e-posta mesajının artık gönderilip gönderilemeyeceğini kontrol ediyorum.
    public bool IsExpired(DateTime utcNow) => ExpiresAt.HasValue && ExpiresAt.Value <= utcNow;

    // Burada başarıyla gönderilen e-postayı yeniden işlenmeyecek şekilde tamamlıyorum.
    public void MarkProcessed(DateTime utcNow)
    {
        ProcessedAt = utcNow;
        LastError = null;
    }

    // Burada başarısız gönderimi kaydedip sonraki denemeyi artan aralıkla planlıyorum.
    public void RecordFailure(DateTime utcNow, string error)
    {
        AttemptCount++;
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Email delivery failed."
            : error[..Math.Min(error.Length, 1000)];
        NextAttemptAt = utcNow.AddMinutes(Math.Min(60, Math.Pow(2, AttemptCount)));
    }

    // Burada kuyrukta tutulacak alıcı adresini tek biçime getiriyorum.
    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Outbox email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }
}
