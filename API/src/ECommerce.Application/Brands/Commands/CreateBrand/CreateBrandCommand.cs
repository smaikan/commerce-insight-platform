using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Commands.CreateBrand;

// Burada marka oluşturma isteğinin alanlarını tanımlıyorum.
public sealed record CreateBrandCommand(
    string Name,
    string? Url = null,
    string? Description = null,
    bool IsActive = true,
    string? ImageUrl = null) : IRequest<BrandDto>;
