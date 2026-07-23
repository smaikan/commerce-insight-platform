using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.ErrorHandling;

public static class ApiProblemDetailsResponse
{
    public static ProblemDetails Create(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string errorCode)
    {
        var problemDetails = new ProblemDetails
        {
            Type = $"urn:ecommerce:error:{errorCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        Enrich(problemDetails, httpContext, errorCode);
        return problemDetails;
    }

    public static void Enrich(ProblemDetails problemDetails, HttpContext httpContext, string? errorCode = null)
    {
        problemDetails.Instance ??= httpContext.Request.Path;
        if (errorCode is not null)
        {
            problemDetails.Extensions["code"] = errorCode;
        }
        else
        {
            problemDetails.Extensions.TryAdd("code", GetErrorCode(problemDetails.Status));
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions.TryAdd("timestamp", DateTimeOffset.UtcNow);
    }

    public static async Task WriteAsync(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        CancellationToken cancellationToken = default)
    {
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var wasWritten = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });

        if (wasWritten)
        {
            return;
        }

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    }

    public static (string Title, string Detail, string Code) DescribeStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ("Bad request", "The request could not be processed.", ApiErrorCodes.BadRequest),
        StatusCodes.Status401Unauthorized => ("Authentication required", "A valid access token is required.", ApiErrorCodes.AuthenticationRequired),
        StatusCodes.Status403Forbidden => ("Forbidden", "You do not have permission to perform this operation.", ApiErrorCodes.Forbidden),
        StatusCodes.Status404NotFound => ("Resource not found", "The requested resource was not found.", ApiErrorCodes.NotFound),
        StatusCodes.Status405MethodNotAllowed => ("Method not allowed", "The HTTP method is not supported for this resource.", ApiErrorCodes.BadRequest),
        StatusCodes.Status429TooManyRequests => ("Too many requests", "The request limit has been exceeded. Please try again later.", ApiErrorCodes.RateLimitExceeded),
        _ => ("Request failed", "The request could not be completed.", ApiErrorCodes.Internal)
    };

    private static string GetErrorCode(int? statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ApiErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => ApiErrorCodes.AuthenticationRequired,
        StatusCodes.Status403Forbidden => ApiErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ApiErrorCodes.NotFound,
        StatusCodes.Status409Conflict => ApiErrorCodes.Conflict,
        StatusCodes.Status429TooManyRequests => ApiErrorCodes.RateLimitExceeded,
        _ => ApiErrorCodes.Internal
    };
}
