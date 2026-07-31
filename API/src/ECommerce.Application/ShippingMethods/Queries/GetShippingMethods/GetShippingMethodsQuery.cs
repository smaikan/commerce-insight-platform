using ECommerce.Application.Common.Models;
using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Queries.GetShippingMethods;

// Burada kargo yöntemlerini sayfalama ve isteğe bağlı aktiflik filtresiyle okuma isteğini taşıyorum.
public sealed record GetShippingMethodsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<PagedResult<ShippingMethodDto>>;
