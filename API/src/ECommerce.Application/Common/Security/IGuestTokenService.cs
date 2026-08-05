namespace ECommerce.Application.Common.Security;

public interface IGuestTokenService
{
    // Burada veritabanında yalnız hash'i tutulacak 256 bitlik guest güvenlik tokenı üretme sözleşmesini tanımlıyorum.
    GuestSecurityToken CreateToken();

    // Burada cart, e-posta, idempotency ve istek değerlerini loglanamaz sabit hash'e dönüştürme sözleşmesini tanımlıyorum.
    string Hash(string value);
}

// Burada istemciye verilecek geçici ham token ile kalıcı depoya yazılacak hash'i birlikte taşıyorum.
public sealed record GuestSecurityToken(string RawValue, string Hash);

public interface IGuestOrderAccessTokenProtector
{
    // Burada outbox'ta tutulacak magic-link tokenını uygulama Data Protection anahtarıyla koruyorum.
    string Protect(string rawToken);

    // Burada e-posta gönderimi sırasında korunan magic-link tokenını geri çözüyorum.
    string Unprotect(string protectedToken);
}
