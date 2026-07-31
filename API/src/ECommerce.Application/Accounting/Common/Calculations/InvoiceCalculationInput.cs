using ECommerce.Domain.Accounting.Common.Enums;

namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada fatura satırlarını dış değişikliklerden koruyarak opsiyonel fatura indirimiyle hesap motoruna taşıyorum.
public sealed class InvoiceCalculationInput
{
    // Burada çağıranın koleksiyonu sonradan değiştirmesini engellemek için satırların savunmacı kopyasını alıyorum.
    public InvoiceCalculationInput(
        IEnumerable<InvoiceLineCalculationInput> lines,
        DiscountCalculationInput? invoiceDiscount = null)
    {
        ArgumentNullException.ThrowIfNull(lines);
        Lines = Array.AsReadOnly(lines.ToArray());
        InvoiceDiscount = invoiceDiscount;
    }

    public IReadOnlyList<InvoiceLineCalculationInput> Lines { get; }
    public DiscountCalculationInput? InvoiceDiscount { get; }
}

// Burada kullanıcının girdiği indirim tanımını hesaplanan indirim tutarlarından ayrı taşıyorum.
public sealed record DiscountCalculationInput(
    DiscountScope Scope,
    DiscountType Type,
    decimal Value,
    DiscountTaxBasis TaxBasis,
    DiscountUnitBasis? UnitBasis = null);
