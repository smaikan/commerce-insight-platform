using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Queries.GetBrandById;

public sealed record GetBrandByIdQuery(Guid Id) : IRequest<BrandDto>;
