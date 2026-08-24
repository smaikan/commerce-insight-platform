using ECommerce.API.Controllers.Product;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada atomik varyant batch sözleşmesinin concurrency, rollback ve hata kodlarını OpenAPI'ye açıklıyorum.
public sealed class ProductVariantBulkOperationFilter : IOperationFilter
{
    // Burada yalnız batch varyant action'ına wire semantiği ve kararlı ProblemDetails kodlarını ekliyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ProductVariantsController) ||
            context.MethodInfo.Name != nameof(ProductVariantsController.BulkUpdate))
        {
            return;
        }

        operation.Description =
            "AdminOnly. Aynı ürüne ait mevcut varyantları tek serializable transaction içinde atomik günceller. " +
            "SKU takası ve döngüsel SKU değişimleri desteklenir; bütün satırlar başarılı olmazsa hiçbir değişiklik kalıcılaşmaz. " +
            "Her satır güncel expectedConcurrencyToken taşır. Başarılı cevap request sırasındaki authoritative varyant listesidir. " +
            "Aynı request eski tokenlarla tekrarlandığında yan etkiler tekrarlanmaz ve concurrency_conflict döner.";

        SetResponseDescription(operation, "200", "Atomik güncelleme tamamlandı; güncel ProductVariantDto listesi döner.");
        SetResponseDescription(operation, "400", "validation_error veya business_rule_violation.");
        SetResponseDescription(operation, "401", "authentication_required veya invalid_access_token.");
        SetResponseDescription(operation, "403", "forbidden.");
        SetResponseDescription(operation, "404", "resource_not_found; bir veya daha fazla varyant belirtilen ürüne ait değildir.");
        SetResponseDescription(operation, "409", "concurrency_conflict veya product_variant_sku_conflict. SKU çakışmasında errors anahtarları variants[n].sku biçimindedir.");
        SetResponseDescription(operation, "500", "internal_error.");

        if (context.SchemaRepository.Schemas.TryGetValue(
                nameof(BulkUpdateProductVariantRequestItem),
                out var itemSchema) &&
            itemSchema is OpenApiSchema mutableItemSchema)
        {
            SetPropertyDescription(
                mutableItemSchema,
                "expectedConcurrencyToken",
                "GET response'undaki güncel concurrencyToken. Eski değer bütün batch'i 409 concurrency_conflict ile rollback eder.");
            SetPropertyDescription(
                mutableItemSchema,
                "stock",
                "Hedef mutlak stok bakiyesi. Fark yalnız bir StockCountAdjustment hareketi olarak kaydedilir.");
        }

        if (context.SchemaRepository.Schemas.TryGetValue(
                nameof(ECommerce.Application.Products.Dtos.ProductVariantDto),
                out var variantSchema) &&
            variantSchema is OpenApiSchema mutableVariantSchema)
        {
            SetPropertyDescription(
                mutableVariantSchema,
                "concurrencyToken",
                "Bir sonraki batch güncellemesinde expectedConcurrencyToken olarak gönderilecek güncel optimistic concurrency değeri.");
        }
    }

    // Burada bir OpenAPI response açıklamasını varsa güvenli biçimde güncelliyorum.
    private static void SetResponseDescription(OpenApiOperation operation, string statusCode, string description)
    {
        if (operation.Responses?.TryGetValue(statusCode, out var response) == true &&
            response is OpenApiResponse mutableResponse)
        {
            mutableResponse.Description = description;
        }
    }

    // Burada request alanının frontend davranışı için gerekli açıklamasını şemaya ekliyorum.
    private static void SetPropertyDescription(OpenApiSchema schema, string propertyName, string description)
    {
        if (schema.Properties?.TryGetValue(propertyName, out var property) == true &&
            property is OpenApiSchema mutableProperty)
        {
            mutableProperty.Description = description;
        }
    }
}
