namespace ECommerce.Domain.Accounting.Common;

// Burada bütün Accounting hesaplarında kullanılacak ortak ondalık hassasiyetleri tanımlıyorum.
public static class AccountingPrecision
{
    public const int UnitPriceScale = 4;
    public const int QuantityScale = 4;
    public const int PercentageScale = 4;
    public const int ExchangeRateScale = 6;
    public const int InvoiceTotalScale = 2;
    public const decimal MaximumExchangeRate = 999_999_999_999.999999m;
    public const decimal MaximumInvoiceAmount = 9_999_999_999_999_999.99m;
    public const MidpointRounding RoundingMode = MidpointRounding.AwayFromZero;
}
