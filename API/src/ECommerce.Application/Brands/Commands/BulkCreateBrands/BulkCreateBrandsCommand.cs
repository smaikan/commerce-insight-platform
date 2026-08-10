using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Commands.BulkCreateBrands;

// Burada toplu marka oluşturma isteğini tanımlıyorum.
public sealed record BulkCreateBrandsCommand(
    IReadOnlyList<BulkCreateBrandItem> Brands) : IRequest<IReadOnlyList<BrandDto>>;

// Burada toplu istekteki tek marka kaydının alanlarını tanımlıyorum.
public sealed record BulkCreateBrandItem(
    string Name,
    string? Url = null,
    string? Description = null,
    bool IsActive = true,
    string? ImageUrl = null);
