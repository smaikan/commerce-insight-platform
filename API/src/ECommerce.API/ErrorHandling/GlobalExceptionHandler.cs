using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    // Burada uygulama hatalarını tutarlı Problem Details cevaplarına çeviriyorum.
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errorCode) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", ApiErrorCodes.Validation),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violation", ApiErrorCodes.BusinessRule),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", ApiErrorCodes.NotFound),
            ConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict", ApiErrorCodes.Concurrency),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", ApiErrorCodes.Conflict),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", ApiErrorCodes.Unauthorized),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad request", ApiErrorCodes.BadRequest),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", ApiErrorCodes.Internal)
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "İşlenmeyen API hatası oluştu. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "API isteği {StatusCode} durum koduyla sonuçlandı. TraceId: {TraceId}",
                statusCode,
                httpContext.TraceIdentifier);
        }

        var detail = statusCode == StatusCodes.Status500InternalServerError
            ? _environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Use the traceId when contacting support."
            : exception.Message;

        ProblemDetails problemDetails;

        if (exception is ValidationException validationException)
        {
            problemDetails = new ValidationProblemDetails(
                validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).Distinct().ToArray()))
            {
                Type = $"urn:ecommerce:error:{errorCode}",
                Status = statusCode,
                Title = title,
                Detail = "One or more validation errors occurred.",
                Instance = httpContext.Request.Path
            };
            ApiProblemDetailsResponse.Enrich(problemDetails, httpContext, errorCode);
        }
        else
        {
            problemDetails = ApiProblemDetailsResponse.Create(
                httpContext,
                statusCode,
                title,
                detail,
                errorCode);
        }

        if (_environment.IsDevelopment() && statusCode == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
        }

        await ApiProblemDetailsResponse.WriteAsync(httpContext, problemDetails, cancellationToken);
        return true;
    }
}
