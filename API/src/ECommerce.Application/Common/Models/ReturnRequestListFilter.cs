using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Models;

// Burada kullanıcı veya yönetim iade listesi için güvenli filtre ve sayfalama değerlerini taşıyorum.
public sealed record ReturnRequestListFilter(
    int PageNumber,
    int PageSize,
    long? UserId = null,
    Guid? OrderId = null,
    ReturnType? Type = null,
    ReturnRequestStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null);
