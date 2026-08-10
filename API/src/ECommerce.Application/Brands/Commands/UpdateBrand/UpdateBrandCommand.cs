using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Commands.UpdateBrand;

// Burada marka güncelleme isteğinin alanlarını tanımlıyorum.
public sealed record UpdateBrandCommand(
    Guid Id,
    string Name,
    string? Url = null,
    string? Description = null,
    string? ImageUrl = null) : IRequest<BrandDto>;
