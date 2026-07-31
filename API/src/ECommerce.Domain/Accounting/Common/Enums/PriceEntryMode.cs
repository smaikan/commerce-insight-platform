namespace ECommerce.Domain.Accounting.Common.Enums;

// Burada girilen birim fiyatın KDV hariç mi KDV dahil mi olduğunu tanımlıyorum.
public enum PriceEntryMode
{
    ExcludingVat = 1,
    IncludingVat = 2
}
