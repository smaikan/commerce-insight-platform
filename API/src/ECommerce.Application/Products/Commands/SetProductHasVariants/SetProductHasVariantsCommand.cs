using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.SetProductHasVariants;

public sealed record SetProductHasVariantsCommand(long Id, bool HasVariants) : IRequest<ProductDto>;
