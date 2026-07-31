namespace ECommerce.Domain.Accounting.Common.Enums;

// Burada muhasebe belgelerinin taslak, kesinleşmiş ve iptal edilmiş yaşam döngüsünü tanımlıyorum.
public enum InvoiceStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3
}
