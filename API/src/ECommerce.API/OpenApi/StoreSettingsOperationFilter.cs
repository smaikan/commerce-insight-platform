using ECommerce.API.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada mağaza ayarı operasyonlarının güvenlik, concurrency ve davranış semantiğini OpenAPI'ye açıklıyorum.
public sealed class StoreSettingsOperationFilter : IOperationFilter
{
    // Burada yalnız StoreSettings controller operasyonlarına frontend için gerekli açıklamaları ekliyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(StoreSettingsController))
        {
            return;
        }

        operation.Description = context.MethodInfo.Name switch
        {
            nameof(StoreSettingsController.GetPublic) =>
                "Anonim storefront sözleşmesidir. Yasal/vergi alanlarını içermez; görünürlüğü kapalı iletişim değerleri null döner.",
            nameof(StoreSettingsController.GetAdmin) =>
                "AdminOnly sözleşmesidir. Bütün yönetilebilir alanları ve güncel concurrencyToken değerini döndürür.",
            nameof(StoreSettingsController.UpdateSeo) =>
                "Yalnız SEO ve sosyal bağlantıları günceller. Canonical storefront origin deployment/environment konfigürasyonunda kalır ve bu istekle değiştirilemez.",
            nameof(StoreSettingsController.UpdateStorefront) =>
                "Yalnız storefront çalışma durumu ve katalog tercihlerini günceller. Status: 0 Active, 1 Maintenance, 2 Disabled. DefaultProductSort: 0 Newest, 1 Popularity, 2 DisplayOrder, 3 Title.",
            _ =>
                "Yalnız ilgili ayar bölümünü atomik günceller. expectedConcurrencyToken zorunludur; eski token 409 concurrency_conflict üretir."
        };
    }
}
