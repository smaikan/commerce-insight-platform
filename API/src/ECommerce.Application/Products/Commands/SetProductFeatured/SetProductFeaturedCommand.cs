using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.SetProductFeatured;

public sealed record SetProductFeaturedCommand(Guid Id, bool IsFeatured) : IRequest<ProductDto>;
