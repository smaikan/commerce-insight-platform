using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Commands.CreateShippingMethod;

// Burada yöneticinin yeni kargo yöntemi oluşturma isteğini taşıyorum.
public sealed record CreateShippingMethodCommand(
    string Name,
    decimal FixedFee,
    bool IsActive = true,
    int DisplayOrder = 0) : IRequest<ShippingMethodDto>;
