using ECommerce.Application.Common.Models;
using ECommerce.Application.Coupons.Dtos;
using MediatR;

namespace ECommerce.Application.Coupons.Queries.GetCoupons;

// Burada kupon listesini sayfalama ve isteÄŸe baÄŸlÄ± aktiflik filtresiyle okuma isteÄŸini taÅŸÄ±yorum.
public sealed record GetCouponsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<PagedResult<CouponDto>>;
