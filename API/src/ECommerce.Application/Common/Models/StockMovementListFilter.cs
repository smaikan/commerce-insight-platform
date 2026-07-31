using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Models;

// Burada stok hareketi geçmişinin güvenli filtre ve sayfalama değerlerini taşıyorum.
public sealed record StockMovementListFilter(
    int PageNumber,
    int PageSize,
    Guid? ProductVariantId = null,
    StockMovementDirection? Direction = null,
    StockMovementType? Type = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null);
