namespace ECommerce.Application.Common.Interfaces;

// Burada Admin Panel işlemlerinden sonra Storefront (Next.js) önbelleğini anında geçersiz kılacak servis sözleşmesini tanımlıyorum.
public interface IStorefrontRevalidationService
{
    Task RevalidateAsync(string? tag = null, string? path = null, CancellationToken cancellationToken = default);
    Task RevalidateProductsAsync(CancellationToken cancellationToken = default);
    Task RevalidateBannersAsync(CancellationToken cancellationToken = default);
    Task RevalidateStoreSettingsAsync(CancellationToken cancellationToken = default);
    Task RevalidateCategoriesAsync(CancellationToken cancellationToken = default);
    Task RevalidateCollectionsAsync(CancellationToken cancellationToken = default);
}
