using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.ErrorHandling;

// Burada batch varyant endpointinin alan hatası içerebilen veya içermeyen ortak ProblemDetails şemasını yayımlıyorum.
public sealed class ProductVariantBulkProblemDetails : ProblemDetails
{
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
