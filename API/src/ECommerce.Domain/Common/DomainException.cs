namespace ECommerce.Domain.Common;

public class DomainException : Exception
{
    // Burada domain kuralı ihlalinin güvenli açıklamasını taşıyorum.
    public DomainException(string message)
        : base(message)
    {
    }

    // Burada domain kuralı ihlalini kök hatasıyla birlikte taşıyorum.
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

// Burada iade yaşam döngüsündeki geçersiz durum geçişlerini kararlı API sözleşmesine ayrılabilir biçimde temsil ediyorum.
public sealed class ReturnStatusTransitionException : DomainException
{
    // Burada geçersiz iade geçişinin güvenli açıklamasını taşıyorum.
    public ReturnStatusTransitionException(string message)
        : base(message)
    {
    }
}
