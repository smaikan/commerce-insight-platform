using ECommerce.Domain.Enums;
using System.Text.Json.Serialization;

namespace ECommerce.Application.Payments;

public sealed record CheckoutFormSessionDto(
    Guid PaymentId,
    Guid OrderId,
    PaymentProvider Provider,
    PaymentStatus Status,
    decimal Amount,
    [property: JsonRequired] string? PaymentPageUrl,
    [property: JsonRequired] DateTime? ExpiresAt);

public sealed record CheckoutFormCompletionDto(
    Guid PaymentId,
    Guid OrderId,
    PaymentStatus Status);
