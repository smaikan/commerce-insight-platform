using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.SetProductVariantActivation;

public sealed record SetProductVariantActivationCommand(Guid Id, bool IsActive) : IRequest<ProductVariantDto>;
