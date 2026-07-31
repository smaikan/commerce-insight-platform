using ECommerce.Domain.Accounting.Common;

namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada Accounting değerlerini tek AwayFromZero politikasıyla yuvarlıyorum.
public sealed class AccountingRoundingPolicy : IAccountingRoundingPolicy
{
    // Burada birim fiyatı dört ondalığa yuvarlıyorum.
    public decimal RoundUnitPrice(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.UnitPriceScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada miktarı dört ondalığa yuvarlıyorum.
    public decimal RoundQuantity(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.QuantityScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada yüzdeyi dört ondalığa yuvarlıyorum.
    public decimal RoundPercentage(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.PercentageScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada parasal tutarı iki ondalığa yuvarlıyorum.
    public decimal RoundMoney(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.InvoiceTotalScale,
            AccountingPrecision.RoundingMode);
    }
}
