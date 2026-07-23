using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.SetProductActivation;

public sealed record SetProductActivationCommand(long Id, bool IsActive) : IRequest<ProductDto>;
