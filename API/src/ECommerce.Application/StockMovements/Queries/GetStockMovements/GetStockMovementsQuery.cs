using ECommerce.Application.Common.Models;
using ECommerce.Application.StockMovements.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.StockMovements.Queries.GetStockMovements;

// Burada yönetim ekranının stok hareketi filtrelerini ve sayfalamasını taşıyorum.
public sealed record GetStockMovementsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? ProductVariantId = null,
    StockMovementDirection? Direction = null,
    StockMovementType? Type = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null) : IRequest<PagedResult<StockMovementDto>>;
