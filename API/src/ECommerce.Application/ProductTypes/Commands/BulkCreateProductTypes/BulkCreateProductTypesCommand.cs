using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.BulkCreateProductTypes;

public sealed record BulkCreateProductTypesCommand(
    IReadOnlyList<BulkCreateProductTypeItem> ProductTypes) : IRequest<IReadOnlyList<ProductTypeDto>>;

public sealed record BulkCreateProductTypeItem(
    string Name,
    string? Description = null,
    bool IsActive = true,
    string? ImageUrl = null);
