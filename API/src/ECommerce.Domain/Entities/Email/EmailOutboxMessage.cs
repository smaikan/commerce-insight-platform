using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class EmailOutboxMessage : BaseEntity
{
    private const int MaximumDeduplicationKeyLength = 200;
    private const int MaximumProcessingWorkerLength = 128;
    private const int MaximumOrderNumberLength = 50;
    private const int MaximumReturnNumberLength = 50;
    private const int MaximumStatusLength = 100;
    public const int MaximumDeliveryAttempts = 10;

    public EmailOutboxMessageType Type { get; private set; }
    public string DeduplicationKey { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? RecipientName { get; private set; }
    public string? ProtectedToken { get; private set; }
    public string? OrderNumber { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Status { get; private set; }
    public string? ReturnNumber { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? DeadLetteredAt { get; private set; }
    public string? LastError { get; private set; }
    public Guid? ClaimToken { get; private set; }
    public string? ProcessingWorker { get; private set; }
    public DateTime? LeaseExpiresAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

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

        return CreateMessage(
            EmailOutboxMessageType.PasswordReset,
            email,
            CreateRandomDeduplicationKey("password-reset"),
            createdAt,
            protectedToken: protectedToken,
            expiresAt: expiresAt);
    }

    // Burada yeni kayıt için gönderilecek hoş geldin e-postasını kuyruğa hazırlıyorum.
    public static EmailOutboxMessage CreateWelcome(string email, string recipientName, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new DomainException("Email recipient name is required.");
        }

        return CreateMessage(
            EmailOutboxMessageType.Welcome,
            email,
            CreateRandomDeduplicationKey("welcome"),
            createdAt,
            recipientName: recipientName);
    }

    // Burada sipariş oluşturulduğunda tekilleştirilmiş müşteri bildirimini hazırlıyorum.
    public static EmailOutboxMessage CreateOrderCreated(
        string email,
        string recipientName,
        Guid orderId,
        string orderNumber,
        decimal grandTotal,
        DateTime createdAt)
    {
        EnsureNonEmptyIdentifier(orderId, "Order id");
        EnsureNonNegativeAmount(grandTotal);

        return CreateMessage(
            EmailOutboxMessageType.OrderCreated,
            email,
            $"order-created:{orderId:N}",
            createdAt,
            recipientName: recipientName,
            orderNumber: orderNumber,
            amount: grandTotal);
    }

    // Burada başarılı ödeme için ödeme kaydına bağlı tekilleştirilmiş müşteri bildirimini hazırlıyorum.
    public static EmailOutboxMessage CreatePaymentPaid(
        string email,
        string recipientName,
        Guid paymentId,
        string orderNumber,
        decimal amount,
        DateTime createdAt)
    {
        EnsureNonEmptyIdentifier(paymentId, "Payment id");
        EnsurePositiveAmount(amount);

        return CreateMessage(
            EmailOutboxMessageType.PaymentPaid,
            email,
            $"payment-paid:{paymentId:N}",
            createdAt,
            recipientName: recipientName,
            orderNumber: orderNumber,
            amount: amount);
    }

    // Burada başarısız ödeme için ödeme kaydına bağlı tekilleştirilmiş müşteri bildirimini hazırlıyorum.
    public static EmailOutboxMessage CreatePaymentFailed(
        string email,
        string recipientName,
        Guid paymentId,
        string orderNumber,
        decimal amount,
        DateTime createdAt)
    {
        EnsureNonEmptyIdentifier(paymentId, "Payment id");
        EnsurePositiveAmount(amount);

        return CreateMessage(
            EmailOutboxMessageType.PaymentFailed,
            email,
            $"payment-failed:{paymentId:N}",
            createdAt,
            recipientName: recipientName,
            orderNumber: orderNumber,
            amount: amount);
    }

    // Burada sipariş durum değişimi için durum başına yalnız bir müşteri bildirimi hazırlıyorum.
    public static EmailOutboxMessage CreateOrderStatusChanged(
        string email,
        string recipientName,
        Guid orderId,
        string orderNumber,
        OrderStatus status,
        DateTime createdAt)
    {
        EnsureNonEmptyIdentifier(orderId, "Order id");

        return CreateMessage(
            EmailOutboxMessageType.OrderStatusChanged,
            email,
            $"order-status:{orderId:N}:{status}",
            createdAt,
            recipientName: recipientName,
            orderNumber: orderNumber,
            status: status.ToString());
    }

    // Burada iade talebinin açıldığını tekilleştirilmiş müşteri bildirimi olarak hazırlıyorum.
    public static EmailOutboxMessage CreateReturnRequested(
        string email,
        string recipientName,
        Guid returnId,
        string orderNumber,
        string returnNumber,
        DateTime createdAt)
    {
        EnsureNonEmptyIdentifier(returnId, "Return id");

        return CreateMessage(
            EmailOutboxMessageType.ReturnRequested,
            email,
            $"return-requested:{returnId:N}",
            createdAt,
            recipientName: recipientName,
            orderNumber: orderNumber,
            returnNumber: returnNumber);
    }

    // Burada iade durum değişimi için durum başına yalnız bir müşteri bildirimi hazırlıyorum.
    public static EmailOutboxMessage CreateReturnStatusChanged(
        string email,
        string recipientName,
        Guid returnId,
        string orderNumber,
        string returnNumber,
        string status,
        DateTime createdAt)
    {
        EnsureNonEmptyIdentifier(returnId, "Return id");
        var normalizedStatus = NormalizeStatus(status);

        return CreateMessage(
            EmailOutboxMessageType.ReturnStatusChanged,
            email,
            $"return-status:{returnId:N}:{normalizedStatus}",
            createdAt,
            recipientName: recipientName,
            orderNumber: orderNumber,
            returnNumber: returnNumber,
            status: normalizedStatus);
    }

    // Burada süresi olan e-posta mesajının artık gönderilip gönderilemeyeceğini kontrol ediyorum.
    public bool IsExpired(DateTime utcNow) => ExpiresAt.HasValue && ExpiresAt.Value <= utcNow;

    // Burada mesajın yeniden denenebilir ve başka bir worker tarafından alınabilir durumda olup olmadığını belirliyorum.
    public bool IsEligibleForClaim(DateTime utcNow)
    {
        return ProcessedAt is null &&
               DeadLetteredAt is null &&
               NextAttemptAt <= utcNow &&
               !IsExpired(utcNow) &&
               (!LeaseExpiresAt.HasValue || LeaseExpiresAt.Value <= utcNow);
    }

    // Burada mesajı belirli worker ve benzersiz claim anahtarı için süreli olarak kilitliyorum.
    public void ClaimForProcessing(
        string workerId,
        Guid claimToken,
        DateTime leaseExpiresAt,
        DateTime utcNow)
    {
        if (!IsEligibleForClaim(utcNow))
        {
            throw new DomainException("Email outbox message is not eligible for processing.");
        }

        if (claimToken == Guid.Empty)
        {
            throw new DomainException("Outbox claim token is required.");
        }

        if (leaseExpiresAt <= utcNow)
        {
            throw new DomainException("Outbox lease expiry must be in the future.");
        }

        var boundedLeaseExpiry = BoundLeaseToMessageExpiry(leaseExpiresAt, utcNow);
        ProcessingWorker = NormalizeProcessingWorker(workerId);
        ClaimToken = claimToken;
        LeaseExpiresAt = boundedLeaseExpiry;
        RefreshConcurrencyToken();
    }

    // Burada işlemi tamamlayacak worker'ın halen geçerli claim sahibi olduğunu doğruluyorum.
    public bool HasActiveClaim(string workerId, Guid claimToken, DateTime utcNow)
    {
        return claimToken != Guid.Empty &&
               ClaimToken == claimToken &&
               string.Equals(ProcessingWorker, workerId.Trim(), StringComparison.Ordinal) &&
               LeaseExpiresAt.HasValue &&
               LeaseExpiresAt.Value > utcNow &&
               ProcessedAt is null &&
               DeadLetteredAt is null;
    }

    // Burada SMTP çağrısından hemen önce aktif claim'in lease süresini uzatıp kuyruk beklemesinde sahiplik kaybını önlüyorum.
    public bool RenewClaim(
        string workerId,
        Guid claimToken,
        DateTime leaseExpiresAt,
        DateTime utcNow)
    {
        if (!HasActiveClaim(workerId, claimToken, utcNow))
        {
            return false;
        }

        if (IsExpired(utcNow))
        {
            return false;
        }

        if (leaseExpiresAt <= utcNow)
        {
            throw new DomainException("Outbox renewed lease expiry must be in the future.");
        }

        LeaseExpiresAt = BoundLeaseToMessageExpiry(leaseExpiresAt, utcNow);
        RefreshConcurrencyToken();
        return true;
    }

    // Burada başarıyla gönderilen e-postayı yeniden işlenmeyecek şekilde tamamlıyorum.
    public void MarkProcessed(DateTime utcNow)
    {
        ProcessedAt = utcNow;
        LastError = null;
        ClearClaim();
        RefreshConcurrencyToken();
    }

    // Burada başarısız gönderimi kaydedip sonraki denemeyi artan aralıkla planlıyorum.
    public void RecordFailure(DateTime utcNow, string error)
    {
        AttemptCount++;
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Email delivery failed."
            : error[..Math.Min(error.Length, 1000)];
        ClearClaim();
        if (AttemptCount >= MaximumDeliveryAttempts)
        {
            DeadLetteredAt = utcNow;
            NextAttemptAt = DateTime.MaxValue;
        }
        else
        {
            NextAttemptAt = utcNow.AddMinutes(Math.Min(60, Math.Pow(2, AttemptCount)));
        }

        RefreshConcurrencyToken();
    }

    // Burada süresi geçen teslim edilmemiş mesajı tekrar denenmeyecek terminal dead-letter durumuna alıyorum.
    public void MarkExpired(DateTime utcNow)
    {
        if (ProcessedAt is not null)
        {
            throw new DomainException("A processed email outbox message cannot be expired.");
        }

        if (DeadLetteredAt is not null)
        {
            return;
        }

        if (!IsExpired(utcNow))
        {
            throw new DomainException("Only an expired email outbox message can be terminally expired.");
        }

        DeadLetteredAt = utcNow;
        LastError = "Email delivery was skipped because the message expired.";
        NextAttemptAt = DateTime.MaxValue;
        ClearClaim();
        RefreshConcurrencyToken();
    }

    // Burada tip-bazlı ve güvenilir alanlarla yeni e-posta outbox kaydını oluşturuyorum.
    private static EmailOutboxMessage CreateMessage(
        EmailOutboxMessageType type,
        string email,
        string deduplicationKey,
        DateTime createdAt,
        string? recipientName = null,
        string? protectedToken = null,
        DateTime? expiresAt = null,
        string? orderNumber = null,
        decimal? amount = null,
        string? status = null,
        string? returnNumber = null)
    {
        return new EmailOutboxMessage
        {
            Type = type,
            DeduplicationKey = NormalizeDeduplicationKey(deduplicationKey),
            Email = NormalizeEmail(email),
            RecipientName = recipientName is null ? null : NormalizeRecipientName(recipientName),
            ProtectedToken = protectedToken,
            ExpiresAt = expiresAt,
            OrderNumber = orderNumber is null ? null : NormalizeOrderNumber(orderNumber),
            Amount = amount,
            Status = status is null ? null : NormalizeStatus(status),
            ReturnNumber = returnNumber is null ? null : NormalizeReturnNumber(returnNumber),
            CreatedAt = createdAt,
            NextAttemptAt = createdAt,
            ConcurrencyToken = Guid.NewGuid()
        };
    }

    // Burada tekrar denenmesi serbest olan kullanıcı güvenlik e-postaları için benzersiz anahtar üretiyorum.
    private static string CreateRandomDeduplicationKey(string prefix)
    {
        return $"{prefix}:{Guid.NewGuid():N}";
    }

    // Burada kısa ömürlü mesajların claim lease'ini token geçerliliğinin ötesine taşımıyorum.
    private DateTime BoundLeaseToMessageExpiry(DateTime requestedLeaseExpiry, DateTime utcNow)
    {
        var boundedLeaseExpiry = ExpiresAt.HasValue && ExpiresAt.Value < requestedLeaseExpiry
            ? ExpiresAt.Value
            : requestedLeaseExpiry;
        if (boundedLeaseExpiry <= utcNow)
        {
            throw new DomainException("Outbox lease expiry must remain before the message expiration.");
        }

        return boundedLeaseExpiry;
    }

    // Burada event kimliği olarak kullanılacak GUID değerinin boş olmadığını doğruluyorum.
    private static void EnsureNonEmptyIdentifier(Guid identifier, string fieldName)
    {
        if (identifier == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }

    // Burada sipariş toplamı için negatif olmayan para değerini doğruluyorum.
    private static void EnsureNonNegativeAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainException("Outbox amount cannot be negative.");
        }
    }

    // Burada ödeme bildirimi için pozitif para değerini doğruluyorum.
    private static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Outbox payment amount must be greater than zero.");
        }
    }

    // Burada outbox tekrar anahtarını indeks sınırına uygun biçimde doğruluyorum.
    private static string NormalizeDeduplicationKey(string deduplicationKey)
    {
        if (string.IsNullOrWhiteSpace(deduplicationKey))
        {
            throw new DomainException("Outbox deduplication key is required.");
        }

        var normalizedKey = deduplicationKey.Trim();

        if (normalizedKey.Length > MaximumDeduplicationKeyLength)
        {
            throw new DomainException("Outbox deduplication key is too long.");
        }

        return normalizedKey;
    }

    // Burada müşteri adını e-posta template'inde güvenle kullanılacak sınırda hazırlıyorum.
    private static string NormalizeRecipientName(string recipientName)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new DomainException("Email recipient name is required.");
        }

        var normalizedName = recipientName.Trim();

        if (normalizedName.Length > 200)
        {
            throw new DomainException("Email recipient name is too long.");
        }

        return normalizedName;
    }

    // Burada sipariş numarasını bildirimde saklanacak sınırda doğruluyorum.
    private static string NormalizeOrderNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("Order number is required.");
        }

        var normalizedOrderNumber = orderNumber.Trim();

        if (normalizedOrderNumber.Length > MaximumOrderNumberLength)
        {
            throw new DomainException("Order number is too long.");
        }

        return normalizedOrderNumber;
    }

    // Burada iade numarasını bildirimde saklanacak sınırda doğruluyorum.
    private static string NormalizeReturnNumber(string returnNumber)
    {
        if (string.IsNullOrWhiteSpace(returnNumber))
        {
            throw new DomainException("Return number is required.");
        }

        var normalizedReturnNumber = returnNumber.Trim();

        if (normalizedReturnNumber.Length > MaximumReturnNumberLength)
        {
            throw new DomainException("Return number is too long.");
        }

        return normalizedReturnNumber;
    }

    // Burada durum metnini e-posta kaydı için boş olmayan ve sınırlı hale getiriyorum.
    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainException("Outbox status is required.");
        }

        var normalizedStatus = status.Trim();

        if (normalizedStatus.Length > MaximumStatusLength)
        {
            throw new DomainException("Outbox status is too long.");
        }

        return normalizedStatus;
    }

    // Burada worker kimliğini lease sahipliği için boş olmayan ve sınırlı hale getiriyorum.
    private static string NormalizeProcessingWorker(string workerId)
    {
        if (string.IsNullOrWhiteSpace(workerId))
        {
            throw new DomainException("Outbox processing worker is required.");
        }

        var normalizedWorkerId = workerId.Trim();

        if (normalizedWorkerId.Length > MaximumProcessingWorkerLength)
        {
            throw new DomainException("Outbox processing worker is too long.");
        }

        return normalizedWorkerId;
    }

    // Burada teslimat durum değişimlerinde eşzamanlı yazıları ayırt edecek yeni sürüm değeri üretiyorum.
    private void RefreshConcurrencyToken()
    {
        ConcurrencyToken = Guid.NewGuid();
    }

    // Burada tamamlanan veya başarısız olan mesajın worker lease bilgisini temizliyorum.
    private void ClearClaim()
    {
        ClaimToken = null;
        ProcessingWorker = null;
        LeaseExpiresAt = null;
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
