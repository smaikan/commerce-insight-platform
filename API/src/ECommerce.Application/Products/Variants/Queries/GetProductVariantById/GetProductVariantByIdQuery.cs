using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantById;

public sealed record GetProductVariantByIdQuery(Guid Id) : IRequest<ProductVariantDto>;
