using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantsByProductId;

public sealed record GetProductVariantsByProductIdQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductVariantDto>>;
