using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Commands.UpdateShippingMethod;

// Burada yöneticinin mevcut kargo yöntemini güncelleme isteğini taşıyorum.
public sealed record UpdateShippingMethodCommand(
    Guid Id,
    string Name,
    decimal FixedFee,
    int DisplayOrder) : IRequest<ShippingMethodDto>;
