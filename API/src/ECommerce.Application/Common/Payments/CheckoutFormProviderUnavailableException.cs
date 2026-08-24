namespace ECommerce.Application.Common.Payments;

public sealed class CheckoutFormProviderUnavailableException : Exception
{
    public string? ErrorCode { get; }

    // Burada iyzico'nun ödeme sonucu yerine API seviyesinde ret verdiğini finansal sonuçtan ayrı taşıyorum.
    public CheckoutFormProviderUnavailableException(string message, string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
