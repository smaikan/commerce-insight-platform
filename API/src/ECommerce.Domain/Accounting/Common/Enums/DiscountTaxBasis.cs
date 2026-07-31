namespace ECommerce.Domain.Accounting.Common.Enums;

// Burada indirimin KDV hariç veya KDV dahil tutar üzerinden girildiğini tanımlıyorum.
public enum DiscountTaxBasis
{
    ExcludingVat = 1,
    IncludingVat = 2
}
