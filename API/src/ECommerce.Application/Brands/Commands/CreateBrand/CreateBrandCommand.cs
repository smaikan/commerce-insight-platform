using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Commands.CreateBrand;

public sealed record CreateBrandCommand(
    string Name,
    string? Url = null,
    string? Description = null,
    bool IsActive = true) : IRequest<BrandDto>;
