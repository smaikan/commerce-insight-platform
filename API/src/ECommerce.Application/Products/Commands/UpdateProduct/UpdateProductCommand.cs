using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Title,
    Guid TypeId,
    string? Url = null,
    Guid? BrandId = null,
    string? Description = null,
    int DisplayOrder = 0,
    string? SeoTitle = null,
    string? SeoDescription = null) : IRequest<ProductDto>;
