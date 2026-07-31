using ECommerce.Domain.Accounting.Common.Enums;

namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada tek fatura satırının yalnız güvenilir ham hesap girdilerini taşıyorum.
public sealed record InvoiceLineCalculationInput(
    int LineNumber,
    decimal Quantity,
    decimal UnitsPerUnit,
    decimal EnteredUnitPrice,
    PriceEntryMode PriceEntryMode,
    decimal VatRate,
    DiscountCalculationInput? LineDiscount = null,
    bool IsInvoiceDiscountEligible = true);
