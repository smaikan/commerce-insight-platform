using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.Common.ValueObjects;

public sealed record ExchangeRate
{
    public decimal Value { get; }

    // Burada döviz kurunu pozitif değer, hassasiyet ve desteklenen veritabanı aralığıyla oluşturuyorum.
    public ExchangeRate(decimal value)
    {
        if (value <= 0m)
        {
            throw new DomainException("Exchange rate must be greater than zero.");
        }

        if (decimal.Round(
                value,
                AccountingPrecision.ExchangeRateScale,
                AccountingPrecision.RoundingMode) != value)
        {
            throw new DomainException(
                $"Exchange rate cannot have more than {AccountingPrecision.ExchangeRateScale} decimal places.");
        }

        if (value > AccountingPrecision.MaximumExchangeRate)
        {
            throw new DomainException("Exchange rate exceeds the supported monetary limit.");
        }

        Value = value;
    }
}
