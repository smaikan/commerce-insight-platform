using ECommerce.Domain.Entities;

namespace ECommerce.Application.TaxRates.Dtos;

// Burada vergi oranının yönetim ve ürün seçim ekranlarında kullanılacak cevap modelini tanımlıyorum.
public sealed record TaxRateDto(
    Guid Id,
    string Name,
    decimal Rate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class TaxRateDtoMapping
{
    // Burada domain vergi oranını dış katmanlara taşınabilecek DTO modeline dönüştürüyorum.
    public static TaxRateDto ToDto(this TaxRate taxRate)
    {
        return new TaxRateDto(
            taxRate.Id,
            taxRate.Name,
            taxRate.Rate,
            taxRate.IsActive,
            taxRate.CreatedAt,
            taxRate.UpdatedAt);
    }
}
