using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Commands.BulkCreateBrands;

public sealed record BulkCreateBrandsCommand(
    IReadOnlyList<BulkCreateBrandItem> Brands) : IRequest<IReadOnlyList<BrandDto>>;

public sealed record BulkCreateBrandItem(
    string Name,
    string? Url = null,
    string? Description = null,
    bool IsActive = true);
