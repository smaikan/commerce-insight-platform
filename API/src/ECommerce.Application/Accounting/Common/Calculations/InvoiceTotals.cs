namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada satır sonuçlarından türetilen ortak fatura başlık toplamlarını taşıyorum.
public sealed record InvoiceTotals(
    decimal SubtotalExcludingVat,
    decimal SubtotalIncludingVat,
    decimal LineDiscountTotalExcludingVat,
    decimal LineDiscountTotalIncludingVat,
    decimal InvoiceDiscountTotalExcludingVat,
    decimal InvoiceDiscountTotalIncludingVat,
    decimal TotalDiscountExcludingVat,
    decimal TotalDiscountIncludingVat,
    decimal NetAmountExcludingVat,
    decimal VatTotal,
    decimal GrandTotalIncludingVat);
