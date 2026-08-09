using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? TypeId = null,
    Guid? BrandId = null,
    ProductStatus? Status = null,
    bool? IsActive = null,
    bool? IsFeatured = null,
    ProductSortBy SortBy = ProductSortBy.CreatedAt,
    bool Descending = true) : IRequest<PagedResult<ProductDto>>;
