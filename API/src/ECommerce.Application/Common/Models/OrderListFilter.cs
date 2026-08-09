using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Models;

// Burada kullanıcı veya yönetim sipariş listesi için sayfalama ve güvenli filtre değerlerini taşıyorum.
public sealed record OrderListFilter(
    int PageNumber,
    int PageSize,
    long? UserId = null,
    string? Search = null,
    OrderStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null);
