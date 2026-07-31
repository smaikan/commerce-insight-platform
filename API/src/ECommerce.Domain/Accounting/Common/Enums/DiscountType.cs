namespace ECommerce.Domain.Accounting.Common.Enums;

// Burada desteklenen yüzde ve sabit indirim türlerini tanımlıyorum.
public enum DiscountType
{
    Percentage = 1,
    FixedPerUnit = 2,
    FixedLineTotal = 3,
    FixedInvoiceTotal = 4
}
