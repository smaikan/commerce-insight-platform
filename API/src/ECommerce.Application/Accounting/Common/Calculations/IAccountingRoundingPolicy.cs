namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada Accounting hesaplarının bütün ondalık yuvarlamalarını tek sözleşmede topluyorum.
public interface IAccountingRoundingPolicy
{
    // Burada birim fiyatı ortak dört ondalıklı hassasiyete yuvarlıyorum.
    decimal RoundUnitPrice(decimal value);

    // Burada miktarı ortak dört ondalıklı hassasiyete yuvarlıyorum.
    decimal RoundQuantity(decimal value);

    // Burada yüzdeyi ortak dört ondalıklı hassasiyete yuvarlıyorum.
    decimal RoundPercentage(decimal value);

    // Burada satır ve fatura tutarını ortak iki ondalıklı hassasiyete yuvarlıyorum.
    decimal RoundMoney(decimal value);
}
