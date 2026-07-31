namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada değiştirilemeyen satır koleksiyonu ile güvenilir fatura toplamlarını birlikte taşıyorum.
public sealed record InvoiceCalculationResult(
    IReadOnlyList<InvoiceLineCalculationResult> Lines,
    InvoiceTotals Totals);
