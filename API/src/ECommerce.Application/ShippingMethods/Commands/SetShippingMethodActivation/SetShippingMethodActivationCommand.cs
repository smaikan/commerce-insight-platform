using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Commands.SetShippingMethodActivation;

// Burada yöneticinin kargo yöntemini yeni checkout seçimlerine açma veya kapatma isteğini taşıyorum.
public sealed record SetShippingMethodActivationCommand(Guid Id, bool IsActive) : IRequest<ShippingMethodDto>;
