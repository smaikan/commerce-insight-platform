using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.SetProductTypeActivation;

public sealed record SetProductTypeActivationCommand(Guid Id, bool IsActive) : IRequest<ProductTypeDto>;
