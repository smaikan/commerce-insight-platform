using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Queries.GetShippingMethodById;

// Burada tek kargo yöntemini kimliğiyle okuma isteğini taşıyorum.
public sealed record GetShippingMethodByIdQuery(Guid Id) : IRequest<ShippingMethodDto>;
