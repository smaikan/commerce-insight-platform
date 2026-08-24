using ECommerce.API.Controllers.GuestOrders;
using ECommerce.API.Controllers.Order;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Enums;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada müşteri iptali sagasının 200/202/409, polling ve numeric enum semantiğini OpenAPI'ye taşıyorum.
public sealed class OrderCancellationOperationFilter : IOperationFilter
{
    // Burada yalnız member ve guest cancellation operasyonlarını owner-scope ve finansal sonuçlarıyla açıklıyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isMemberCancel = context.MethodInfo.DeclaringType == typeof(OrdersController) &&
            context.MethodInfo.Name == nameof(OrdersController.Cancel);
        var isGuestCancel = context.MethodInfo.DeclaringType == typeof(GuestOrdersController) &&
            context.MethodInfo.Name == nameof(GuestOrdersController.Cancel);
        var isMemberPoll = context.MethodInfo.DeclaringType == typeof(OrdersController) &&
            context.MethodInfo.Name == nameof(OrdersController.GetCancellation);
        var isGuestPoll = context.MethodInfo.DeclaringType == typeof(GuestOrdersController) &&
            context.MethodInfo.Name == nameof(GuestOrdersController.GetCancellation);
        if (!isMemberCancel && !isGuestCancel && !isMemberPoll && !isGuestPoll)
        {
            EnhanceSchemas(context.SchemaRepository);
            return;
        }

        if (isMemberCancel || isGuestCancel)
        {
            operation.Description =
                "Owner-scoped. Pending/Confirmed siparişte mevcut CheckoutForm mutabakatını; Paid/Preparing siparişte " +
                "iyzico reporting sonrası aynı gün payment cancel veya gerçek item transaction değerleriyle standart refund sagasını kullanır. " +
                "Provider başarısı kesinleşmeden Order/Payment/stok/kupon değiştirilmez. Shipped ve sonrası reddedilir.";
            SetResponseDescription(operation, "200", "İptal tamamlandı veya idempotent replay; güncel OrderDto ve status=Cancelled (6).");
            SetResponseDescription(operation, "202", "Provider sonucu mutabakat bekliyor; owner-scoped polling URL taşıyan OrderCancellationOperationDto.");
            SetResponseDescription(operation, "400", "ProblemDetails code=validation_error veya bad_request.");
            SetResponseDescription(
                operation,
                "409",
                "ProblemDetails code=order_cancellation_not_allowed, payment_reversal_data_missing, " +
                "payment_reversal_rejected, payment_reversal_manual_review veya conflict.");
        }
        else
        {
            operation.Description =
                "Yalnız sipariş sahibinin en güncel kalıcı cancellation/reversal operasyonunu polling için döndürür. " +
                "Provider kimlikleri ve iç hata payload'ı response'a açılmaz.";
            SetResponseDescription(operation, "200", "Güncel OrderCancellationOperationDto.");
        }

        SetResponseDescription(
            operation,
            "401",
            isGuestCancel || isGuestPoll
                ? "ProblemDetails code=invalid_guest_access."
                : "ProblemDetails code=authentication_required veya invalid_access_token.");
        SetResponseDescription(operation, "403", "ProblemDetails code=invalid_guest_access; trusted Origin veya CSRF doğrulaması başarısız.");
        SetResponseDescription(operation, "404", "ProblemDetails code=resource_not_found; owner scope bilgi sızdırmaz.");
        EnhanceSchemas(context.SchemaRepository);
    }

    // Burada cancellation DTO ve enumlarının wire değerleri ile nullable zaman alanını component şemalarında açıklıyorum.
    private static void EnhanceSchemas(SchemaRepository repository)
    {
        SetSchemaDescription(
            repository,
            nameof(OrderCancellationOperationStatus),
            "Numeric wire değerleri: 0 Requested, 1 Processing, 2 ReconciliationPending, 3 Completed, 4 Failed, 5 ManualReview.");
        SetSchemaDescription(
            repository,
            nameof(PaymentReversalType),
            "Numeric wire değerleri: 0 Cancel (aynı gün tam iptal), 1 Refund (standart item-level refund).");
        SetSchemaDescription(
            repository,
            nameof(OrderCancellationOperationDto),
            "Provider kimliklerini açmayan owner-scoped cancellation polling DTO'su. Bütün tarihler UTC'dir; nextAttemptAt nullable'dır.");
        SetPropertyDescription(
            repository,
            nameof(OrderCancellationOperationDto),
            "pollingUrl",
            "Member veya guest sahiplik kontrolünü yeniden uygulayan relative polling URL'si.");
    }

    // Burada mevcut OpenAPI response şemasını bozmadan gerçek status/code açıklamasını güncelliyorum.
    private static void SetResponseDescription(OpenApiOperation operation, string statusCode, string description)
    {
        if (operation.Responses?.TryGetValue(statusCode, out var response) == true &&
            response is OpenApiResponse mutableResponse)
        {
            mutableResponse.Description = description;
        }
    }

    // Burada bulunan component şemasının açıklamasını değişmez numeric sözleşmeyle zenginleştiriyorum.
    private static void SetSchemaDescription(SchemaRepository repository, string schemaName, string description)
    {
        if (repository.Schemas.TryGetValue(schemaName, out var schema) && schema is OpenApiSchema mutableSchema)
        {
            mutableSchema.Description = description;
        }
    }

    // Burada cancellation DTO alan açıklamasını mevcut required/nullable metadata'sını değiştirmeden ekliyorum.
    private static void SetPropertyDescription(
        SchemaRepository repository,
        string schemaName,
        string propertyName,
        string description)
    {
        if (repository.Schemas.TryGetValue(schemaName, out var schema) &&
            schema is OpenApiSchema mutableSchema &&
            mutableSchema.Properties?.TryGetValue(propertyName, out var property) == true &&
            property is OpenApiSchema mutableProperty)
        {
            mutableProperty.Description = description;
        }
    }
}
