using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Commands.UpdateBrand;

public sealed record UpdateBrandCommand(
    Guid Id,
    string Name,
    string? Url = null,
    string? Description = null) : IRequest<BrandDto>;
