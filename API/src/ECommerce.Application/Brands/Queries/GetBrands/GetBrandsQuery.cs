using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Brands.Queries.GetBrands;

public sealed record GetBrandsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<BrandDto>>;
