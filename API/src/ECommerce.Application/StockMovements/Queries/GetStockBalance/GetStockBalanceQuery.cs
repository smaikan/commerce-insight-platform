using ECommerce.Application.StockMovements.Dtos;
using MediatR;

namespace ECommerce.Application.StockMovements.Queries.GetStockBalance;

// Burada seçili varyantın stok hareketi mutabakat sorgusunu taşıyorum.
public sealed record GetStockBalanceQuery(Guid ProductVariantId) : IRequest<StockBalanceDto>;
