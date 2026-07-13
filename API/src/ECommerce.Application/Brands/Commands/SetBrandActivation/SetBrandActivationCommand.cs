using ECommerce.Application.Brands.Dtos;
using MediatR;

namespace ECommerce.Application.Brands.Commands.SetBrandActivation;

public sealed record SetBrandActivationCommand(Guid Id, bool IsActive) : IRequest<BrandDto>;
