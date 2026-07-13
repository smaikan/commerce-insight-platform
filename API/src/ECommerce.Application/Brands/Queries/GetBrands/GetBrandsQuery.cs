using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Queries.GetBrands;

public sealed record GetBrandsQuery : IRequest<IReadOnlyList<BrandDto>>;
