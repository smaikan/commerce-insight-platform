namespace ECommerce.Application.Common.Security;

public sealed record ContactProtectionRequest(
    string NormalizedEmail,
    string? ClientIpAddress,
    string? TurnstileToken);

public interface IContactProtectionService
{
    // Burada iletişim formunun e-posta, güvenilir IP ve challenge korumasını tek sözleşmede değerlendiriyorum.
    Task EvaluateAsync(ContactProtectionRequest request, CancellationToken cancellationToken = default);
}
