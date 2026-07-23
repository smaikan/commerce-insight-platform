using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.DeleteProductVariant;

public sealed record DeleteProductVariantCommand(Guid Id) : IRequest;
