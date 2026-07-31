using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IVariantOptionResolver
{
    // Burada varyant adını ve değerini merkezi kayıtlardan çözüp gerekirse yeni kayıt oluşturmaya yönelik sözleşmeyi tanımlıyorum.
    Task<VariantOptionSelection> ResolveAsync(
        string name,
        string value,
        CancellationToken cancellationToken = default);

    // Burada birleşik string alanlarını en fazla üç merkezi ad-değer seçimine ayırma sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<VariantOptionSelection>> ResolveCompositeAsync(string name, string value, CancellationToken cancellationToken = default);
}
