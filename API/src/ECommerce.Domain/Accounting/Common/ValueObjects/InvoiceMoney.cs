using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.Common.ValueObjects;

public sealed record InvoiceMoney
{
    public decimal Amount { get; }
    public CurrencyCode CurrencyCode { get; }

    // Burada fatura tutarını para birimiyle birlikte negatif olmayan iki ondalıklı güvenli aralıkta oluşturuyorum.
    public InvoiceMoney(decimal amount, CurrencyCode currencyCode)
    {
        if (currencyCode is null)
        {
            throw new DomainException("Invoice money currency code is required.");
        }

        if (amount < 0m)
        {
            throw new DomainException("Invoice money cannot be negative.");
        }

        if (decimal.Round(
                amount,
                AccountingPrecision.InvoiceTotalScale,
                AccountingPrecision.RoundingMode) != amount)
        {
            throw new DomainException(
                $"Invoice money cannot have more than {AccountingPrecision.InvoiceTotalScale} decimal places.");
        }

        if (amount > AccountingPrecision.MaximumInvoiceAmount)
        {
            throw new DomainException("Invoice money exceeds the supported monetary limit.");
        }

        Amount = amount;
        CurrencyCode = currencyCode;
    }
}
