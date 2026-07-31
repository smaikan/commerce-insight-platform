namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada tek satırın KDV hariç ve dahil hesap sonuçlarını ayrı alanlarda taşıyorum.
public sealed record InvoiceLineCalculationResult(
    int LineNumber,
    decimal StockQuantity,
    decimal UnitPriceExcludingVat,
    decimal UnitPriceIncludingVat,
    decimal GrossAmountExcludingVat,
    decimal GrossAmountIncludingVat,
    decimal LineDiscountAmountExcludingVat,
    decimal LineDiscountAmountIncludingVat,
    decimal InvoiceDiscountShareExcludingVat,
    decimal InvoiceDiscountShareIncludingVat,
    decimal TotalDiscountAmountExcludingVat,
    decimal TotalDiscountAmountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal VatAmount,
    decimal TotalAmountIncludingVat);
