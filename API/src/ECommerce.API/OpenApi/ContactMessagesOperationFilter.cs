using ECommerce.API.Controllers.Contact;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada Contact yönetiminin durum, audit ve sıralama davranışlarını OpenAPI sözleşmesine taşıyorum.
public sealed class ContactMessagesOperationFilter : IOperationFilter
{
    private const string StatusTransitions =
        "İzinli geçişler: New (0) → InProgress (1), WaitingForCustomer (2), Closed (4), Spam (5); " +
        "InProgress (1) → WaitingForCustomer (2), Resolved (3), Closed (4), Spam (5); " +
        "WaitingForCustomer (2) → InProgress (1), Resolved (3), Closed (4), Spam (5); " +
        "Resolved (3) → InProgress (1), Closed (4); Closed (4) → InProgress (1); " +
        "Spam (5) → New (0), Closed (4). Aynı durumdan aynı duruma geçiş geçersizdir.";

    // Burada yalnız ContactMessagesController operasyonlarını ve ürettikleri ortak şemaları açıklıyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ContactMessagesController))
        {
            return;
        }

        if (context.MethodInfo.Name == nameof(ContactMessagesController.Submit))
        {
            operation.Description =
                "Başvuru kalıcı ContactMessage ve operasyonel bildirim outbox kaydını aynı transaction içinde oluşturur; SMTP request sırasında çalışmaz.";
            SetResponseDescription(operation, "400", "ProblemDetails code=validation_error veya malformed/model-binding isteğinde bad_request.");
            SetResponseDescription(operation, "409", "ProblemDetails code=idempotency_key_reused; benzersiz referans üretilemezse conflict.");
            SetResponseDescription(operation, "413", "ProblemDetails code=payload_too_large.");
            SetResponseDescription(operation, "428", "ProblemDetails code=contact_challenge_required.");
            SetResponseDescription(operation, "429", "ProblemDetails code=contact_submission_rate_limited; Retry-After header'ı döner.");
            SetResponseDescription(operation, "503", "ProblemDetails code=contact_protection_unavailable.");
        }
        else if (context.MethodInfo.Name == nameof(ContactMessagesController.GetList))
        {
            operation.Description =
                "createdFromUtc ve createdToUtc UTC olmak zorundadır ve iki sınır da inclusive uygulanır. Sonuçlar createdAt DESC, ardından id DESC sıralanır.";
            SetParameterDescription(operation, "CreatedFromUtc", "Opsiyonel UTC alt sınırı; CreatedAt >= createdFromUtc (inclusive).");
            SetParameterDescription(operation, "CreatedToUtc", "Opsiyonel UTC üst sınırı; CreatedAt <= createdToUtc (inclusive).");
            SetResponseDescription(operation, "400", "ProblemDetails code=validation_error veya query model-binding hatasında bad_request.");
        }
        else if (context.MethodInfo.Name == nameof(ContactMessagesController.ChangeStatus))
        {
            operation.Description = StatusTransitions +
                " Geçersiz geçiş 400 business_rule_violation, eski token 409 concurrency_conflict üretir.";
            SetResponseDescription(operation, "400", "ProblemDetails code=validation_error veya bad_request; domain geçiş ihlalinde business_rule_violation.");
            SetResponseDescription(operation, "409", "ProblemDetails code=concurrency_conflict.");
        }
        else if (context.MethodInfo.Name == nameof(ContactMessagesController.GetById))
        {
            operation.Description =
                "Activity ve reply timeline dizileri createdAt ASC, ardından id ASC sırasıyla döner. Activity alan semantiği component şemalarında açıklanır.";
        }
        else if (context.MethodInfo.Name == nameof(ContactMessagesController.ChangeAssignment))
        {
            SetResponseDescription(operation, "400", "ProblemDetails code=validation_error veya bad_request.");
            SetResponseDescription(operation, "409", "ProblemDetails code=concurrency_conflict; hedef kullanıcı aktif Admin değilse conflict.");
        }
        else if (context.MethodInfo.Name == nameof(ContactMessagesController.AddNote))
        {
            SetResponseDescription(operation, "400", "ProblemDetails code=validation_error veya bad_request.");
            SetResponseDescription(operation, "409", "ProblemDetails code=concurrency_conflict.");
        }
        else if (context.MethodInfo.Name == nameof(ContactMessagesController.Reply))
        {
            operation.Description =
                "Reply ve e-posta outbox kaydı aynı transaction içinde kuyruğa alınır; SMTP tamamlanmadan Sent denmez. " +
                "FirstRespondedAt ilk yanıtta set edilir. Mevcut status New veya InProgress ise status WaitingForCustomer olur ve ayrı StatusChanged activity kaydı eklenir; diğer status değerleri değişmez.";
            SetResponseDescription(operation, "400", "ProblemDetails code=validation_error veya malformed/header model-binding isteğinde bad_request; anonimleştirilmiş kayıtta business_rule_violation.");
            SetResponseDescription(operation, "409", "ProblemDetails code=idempotency_key_reused.");
        }

        SetResponseDescription(operation, "401", "ProblemDetails code=authentication_required; geçersiz veya süresi dolmuş token için invalid_access_token.");
        SetResponseDescription(operation, "403", "ProblemDetails code=forbidden.");
        SetResponseDescription(operation, "404", "ProblemDetails code=resource_not_found.");

        EnhanceComponentSchemas(context.SchemaRepository);
    }

    // Burada numeric activity enumunu, koşullu activity alanlarını ve timeline sırasını component şemalarında açıklıyorum.
    private static void EnhanceComponentSchemas(SchemaRepository repository)
    {
        SetSchemaDescription(repository, "ContactMessageStatus", StatusTransitions);
        SetSchemaDescription(
            repository,
            "ContactMessageActivityType",
            "Numeric değerler: 0 Submitted, 1 StatusChanged, 2 AssignmentChanged, 3 InternalNoteAdded, 4 ReplyQueued.");
        SetSchemaDescription(repository, "ChangeContactMessageStatusRequest", StatusTransitions);
        SetPropertyDescription(repository, "ContactMessageDetailDto", "activities", "createdAt ASC, ardından id ASC sıralı append-only activity timeline'ı.");
        SetPropertyDescription(repository, "ContactMessageDetailDto", "replies", "createdAt ASC, ardından id ASC sıralı immutable müşteri yanıtları.");
        SetPropertyDescription(repository, "ContactMessageActivityDto", "actorAdminUserId", "Submitted tipinde null; diğer tiplerde işlemi yapan yöneticinin U... public ID değeri.");
        SetPropertyDescription(repository, "ContactMessageActivityDto", "content", "Yalnız InternalNoteAdded tipinde dahili not; diğer tiplerde null.");
        SetPropertyDescription(repository, "ContactMessageActivityDto", "previousValue", "StatusChanged için önceki enum adı; AssignmentChanged için önceki U... public ID veya atamasızsa null; diğer tiplerde null.");
        SetPropertyDescription(repository, "ContactMessageActivityDto", "newValue", "StatusChanged için yeni enum adı; AssignmentChanged için yeni U... public ID veya atama kaldırıldıysa null; diğer tiplerde null.");
        SetPropertyDescription(repository, "ContactMessageActivityDto", "replyId", "Yalnız ReplyQueued tipinde ilişkili ContactMessageReplyDto.id; diğer tiplerde null.");
    }

    // Burada component şemasının mevcut yapısını değiştirmeden davranış açıklamasını ekliyorum.
    private static void SetSchemaDescription(SchemaRepository repository, string schemaName, string description)
    {
        if (repository.Schemas.TryGetValue(schemaName, out var schema) && schema is OpenApiSchema mutableSchema)
        {
            mutableSchema.Description = description;
        }
    }

    // Burada DTO alan açıklamasını camelCase OpenAPI property şemasına güvenli biçimde yazıyorum.
    private static void SetPropertyDescription(SchemaRepository repository, string schemaName, string propertyName, string description)
    {
        if (repository.Schemas.TryGetValue(schemaName, out var schema) &&
            schema is OpenApiSchema mutableSchema &&
            mutableSchema.Properties?.TryGetValue(propertyName, out var property) == true &&
            property is OpenApiSchema mutableProperty)
        {
            mutableProperty.Description = description;
        }
    }

    // Burada mevcut response şemasını bozmadan gerçek ProblemDetails code değerlerini açıklıyorum.
    private static void SetResponseDescription(OpenApiOperation operation, string statusCode, string description)
    {
        if (operation.Responses?.TryGetValue(statusCode, out var response) == true && response is OpenApiResponse mutableResponse)
        {
            mutableResponse.Description = description;
        }
    }

    // Burada liste tarih filtrelerinin UTC ve inclusive davranışını ilgili query parametresine yazıyorum.
    private static void SetParameterDescription(OpenApiOperation operation, string parameterName, string description)
    {
        var parameter = operation.Parameters?
            .FirstOrDefault(candidate => string.Equals(candidate.Name, parameterName, StringComparison.OrdinalIgnoreCase));
        if (parameter is OpenApiParameter mutableParameter)
        {
            mutableParameter.Description = description;
        }
    }
}
