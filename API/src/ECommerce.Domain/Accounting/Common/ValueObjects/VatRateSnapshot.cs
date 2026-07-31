using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.Common.ValueObjects;

public sealed record VatRateSnapshot
{
    public const decimal MinimumRate = 0m;
    public const decimal MaximumRate = 100m;

    public decimal Rate { get; }

    // Burada tarihsel KDV oranını canlı vergi kaydından bağımsız ve değişmez biçimde oluşturuyorum.
    public VatRateSnapshot(decimal rate)
    {
        if (rate < MinimumRate || rate > MaximumRate)
        {
            throw new DomainException(
                $"VAT rate must be between {MinimumRate} and {MaximumRate}.");
        }

        if (decimal.Round(
                rate,
                AccountingPrecision.PercentageScale,
                AccountingPrecision.RoundingMode) != rate)
        {
            throw new DomainException(
                $"VAT rate cannot have more than {AccountingPrecision.PercentageScale} decimal places.");
        }

        Rate = rate;
    }
}
