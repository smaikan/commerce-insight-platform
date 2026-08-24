using ECommerce.API.Controllers.Returns;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada iade karar sırası, stok etkileri ve kararlı hata kodlarını OpenAPI sözleşmesine taşıyorum.
public sealed class ReturnsOperationFilter : IOperationFilter
{
    private const string NewLifecycle =
        "Yeni kayıt yaşam döngüsü: Requested (0) → Received (3) → Approved (1) veya Rejected (2).";

    // Burada Returns controller operasyonlarını yeni yaşam döngüsü ve sınırlı legacy uyumluluğuyla açıklıyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ReturnsController))
        {
            return;
        }

        if (context.MethodInfo.Name == nameof(ReturnsController.Receive))
        {
            operation.Description = NewLifecycle +
                " Yeni Requested kayıt Received olur, receivedAt UTC yazılır, Order ReturnRequested (8) kalır ve stok değişmez. " +
                "Yalnız deployment öncesinden kalan Approved kayıtların eski receive davranışı geriye dönük uyumluluk için korunur.";
        }
        else if (context.MethodInfo.Name == nameof(ReturnsController.Approve))
        {
            operation.Description = NewLifecycle +
                " Yalnız karar bekleyen Received kayıt onaylanır. Refund talebi Order Refunded (7) yapar ve SaleReturn stok girişini; " +
                "Exchange talebi Order ReturnApproved (9) yapar, iade stok girişini ve replacement stok çıkışını aynı transaction içinde uygular. " +
                "Ödeme sağlayıcısına refund çağrısı yapılmaz ve Payment kaydı değiştirilmez.";
        }
        else if (context.MethodInfo.Name == nameof(ReturnsController.Reject))
        {
            operation.Description = NewLifecycle +
                " Yalnız karar bekleyen Received kayıt reddedilir; stok hareketi oluşmaz ve Order durumu diğer aktif taleplerden yeniden türetilir.";
        }
        else if (context.MethodInfo.Name == nameof(ReturnsController.Complete))
        {
            operation.Description =
                "Yeni kayıtlarda kullanılmaz. Yalnız deployment öncesindeki Approved → Received kayıtlarını Completed (4) durumuna taşıyan sınırlı uyumluluk endpointidir.";
        }
        else
        {
            EnhanceStatusSchema(context.SchemaRepository);
            return;
        }

        SetResponseDescription(operation, "400", "ProblemDetails code=validation_error, bad_request veya business_rule_violation.");
        SetResponseDescription(operation, "401", "ProblemDetails code=authentication_required veya invalid_access_token.");
        SetResponseDescription(operation, "403", "ProblemDetails code=forbidden.");
        SetResponseDescription(operation, "404", "ProblemDetails code=resource_not_found.");
        SetResponseDescription(
            operation,
            "409",
            "Geçersiz yaşam döngüsü geçişinde ProblemDetails code=return_status_transition_invalid; " +
            "gerçek eşzamanlı yazma yarışında concurrency_conflict; stok veya varyant çakışmasında conflict.");
        EnhanceStatusSchema(context.SchemaRepository);
    }

    // Burada numeric ReturnRequestStatus değerlerini ve yeni akıştaki anlamlarını component şemasında açıklıyorum.
    private static void EnhanceStatusSchema(SchemaRepository repository)
    {
        if (repository.Schemas.TryGetValue("ReturnRequestStatus", out var schema) && schema is OpenApiSchema mutableSchema)
        {
            mutableSchema.Description =
                "Numeric değerler değişmez: 0 Requested, 1 Approved, 2 Rejected, 3 Received, 4 Completed. " +
                NewLifecycle + " Completed yalnız legacy uyumluluğu içindir.";
        }
    }

    // Burada mevcut response şemasını bozmadan gerçek ProblemDetails kodlarını açıklıyorum.
    private static void SetResponseDescription(OpenApiOperation operation, string statusCode, string description)
    {
        if (operation.Responses?.TryGetValue(statusCode, out var response) == true && response is OpenApiResponse mutableResponse)
        {
            mutableResponse.Description = description;
        }
    }
}
