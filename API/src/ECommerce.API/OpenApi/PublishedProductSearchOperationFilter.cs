using System.Text.Json.Nodes;
using ECommerce.API.Controllers.Product;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada public arama parametrelerini, hata cevaplarını ve örnek payload'ı OpenAPI'de açıklıyorum.
public sealed class PublishedProductSearchOperationFilter : IOperationFilter
{
    // Burada published liste ve suggestion operasyonlarının arama sözleşmesini zenginleştiriyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ProductsController))
        {
            return;
        }

        if (context.MethodInfo.Name == nameof(ProductsController.GetPublishedSearchSuggestions))
        {
            operation.Description =
                "Navbar otomatik tamamlama endpointidir. Query normalize edildikten sonra 2-100 karakter, " +
                "Limit 1-10 aralığındadır. COUNT çalıştırmaz; Limit+1 kayıtla hasMore üretir ve IP başına dakikada 120 istekle sınırlıdır.";
            foreach (var parameter in operation.Parameters ?? [])
            {
                if (string.Equals(parameter.Name, "Query", StringComparison.OrdinalIgnoreCase))
                {
                    if (parameter is OpenApiParameter mutableParameter)
                    {
                        mutableParameter.Required = true;
                        mutableParameter.Example = JsonValue.Create("şönil");
                    }
                    if (parameter.Schema is OpenApiSchema schema)
                    {
                        schema.MinLength = 2;
                        schema.MaxLength = 100;
                    }
                }
                else if (string.Equals(parameter.Name, "Limit", StringComparison.OrdinalIgnoreCase))
                {
                    if (parameter.Schema is OpenApiSchema schema)
                    {
                        schema.Minimum = "1";
                        schema.Maximum = "10";
                        schema.Default = 10;
                    }
                    if (parameter is OpenApiParameter mutableParameter)
                    {
                        mutableParameter.Example = JsonValue.Create(10);
                    }
                }
            }

            if (operation.Responses?.TryGetValue("200", out var response) == true &&
                response.Content?.TryGetValue("application/json", out var mediaType) == true)
            {
                mediaType.Example = JsonNode.Parse(
                    """
                    {"items":[{"id":"P123","title":"Şönil Taşlı Kolye","url":"sonil-tasli-kolye","brandName":"Marka","price":2499.90,"compareAtPrice":2799.90,"imageUrl":"https://cdn.example.com/product.jpg","imageAlt":"Şönil taşlı kolye","isAvailable":true}],"hasMore":true}
                    """);
            }
            return;
        }

        if (IsPublishedProductListOperation(context.MethodInfo.Name))
        {
            var sortBy = operation.Parameters?.FirstOrDefault(parameter =>
                string.Equals(parameter.Name, "SortBy", StringComparison.OrdinalIgnoreCase));
            if (sortBy is not null)
            {
                sortBy.Description =
                    "Sıralama alanı: 0 Newest, 1 Popularity (ağırlıklı PopularityScore), " +
                    "2 DisplayOrder, 3 Title, 4 BestSelling (kesinleşmiş net satış adedi). " +
                    "Eşitliklerde Product.Id artan uygulanır.";
            }
        }

        if (context.MethodInfo.Name == nameof(ProductsController.GetPublishedList))
        {
            var search = operation.Parameters?.FirstOrDefault(parameter =>
                string.Equals(parameter.Name, "Search", StringComparison.OrdinalIgnoreCase));
            if (search?.Schema is OpenApiSchema schema)
            {
                schema.MaxLength = 100;
            }
            if (search is not null)
            {
                search.Description =
                    "Opsiyonel 2-100 karakterli public ürün araması. SortBy verilmezse relevance uygulanır.";
                if (search is OpenApiParameter mutableParameter)
                {
                    mutableParameter.Example = JsonValue.Create("şönil");
                }
            }
        }
    }

    // Burada ortak sıralama sözleşmesini kullanan beş public ürün listeleme operasyonunu ayırt ediyorum.
    private static bool IsPublishedProductListOperation(string methodName)
    {
        return methodName is nameof(ProductsController.GetPublishedList) or
            nameof(ProductsController.GetPublishedByCollection) or
            nameof(ProductsController.GetPublishedByTag) or
            nameof(ProductsController.GetPublishedByType) or
            nameof(ProductsController.GetPublishedByBrand);
    }
}
