using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.API.OpenApi;

// Burada AllowAnonymous operasyonlarının global Bearer gereksinimini OpenAPI'de kaldırıyorum.
public sealed class AllowAnonymousOperationFilter : IOperationFilter
{
    // Burada anonim action veya controller metadata'sı bulunan operasyonu security boş dizisiyle işaretliyorum.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<IAllowAnonymous>()
            .Any() ||
            context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<IAllowAnonymous>()
                .Any() == true;

        if (isAnonymous)
        {
            operation.Security = [];
        }
    }
}
