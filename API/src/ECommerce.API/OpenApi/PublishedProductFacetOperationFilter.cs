using System.Text.Json.Nodes;
using ECommerce.API.Controllers.Product;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada public facet endpointlerinin sayma semantiğini ve örnek cevabını OpenAPI'ye ekliyorum.
public sealed class PublishedProductFacetOperationFilter : IOperationFilter
{
    // Burada yalnız facet controller operasyonlarının açıklama, parametre ve response örneğini zenginleştiriyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(PublishedProductFacetsController))
        {
            return;
        }

        operation.Description =
            "Yalnız aktif ve yayımlanmış ürünleri sayar. Endpointin kendi facet filtresi " +
            "sayım sırasında dışarıda bırakılır; diğer filtreler AND mantığıyla uygulanır.";

        foreach (var parameter in operation.Parameters ?? [])
        {
            var parameterName = parameter.Name ?? string.Empty;
            if (parameter is OpenApiParameter mutableParameter)
            {
                mutableParameter.Required = false;
            }

            if (parameter.Schema is OpenApiSchema mutableSchema)
            {
                mutableSchema.Type = (mutableSchema.Type ?? JsonSchemaType.String) | JsonSchemaType.Null;
            }

            parameter.Description = parameterName.ToLowerInvariant() switch
            {
                "typeid" => "Opsiyonel ürün türü filtresi; product-types endpointinde sayımdan dışlanır.",
                "brandid" => "Opsiyonel marka filtresi; brands endpointinde sayımdan dışlanır.",
                "collectionid" => "Opsiyonel koleksiyon filtresi; collections endpointinde sayımdan dışlanır.",
                "tagid" => "Opsiyonel etiket filtresi; tüm facet endpointlerinde uygulanır.",
                _ => parameter.Description
            };
        }

        if (operation.Responses is null ||
            !operation.Responses.TryGetValue("200", out var response) ||
            response.Content is null ||
            !response.Content.TryGetValue("application/json", out var mediaType))
        {
            return;
        }

        mediaType.Example = JsonNode.Parse(
            """
            [
              {
                "id": "11111111-1111-1111-1111-111111111111",
                "name": "Marka",
                "productCount": 12
              }
            ]
            """);
    }
}
