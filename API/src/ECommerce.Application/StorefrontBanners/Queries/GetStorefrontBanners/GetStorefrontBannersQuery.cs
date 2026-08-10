using ECommerce.Application.StorefrontBanners.Dtos;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Queries.GetStorefrontBanners;

// Burada tek storefront banner setini okuma isteğini tanımlıyorum.
public sealed record GetStorefrontBannersQuery : IRequest<StorefrontBannersDto>;
