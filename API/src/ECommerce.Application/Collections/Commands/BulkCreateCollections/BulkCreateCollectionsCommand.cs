using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Commands.BulkCreateCollections;

public sealed record BulkCreateCollectionsCommand(
    IReadOnlyList<BulkCreateCollectionItem> Collections) : IRequest<IReadOnlyList<CollectionDto>>;

public sealed record BulkCreateCollectionItem(
    string Name,
    string? Url = null,
    string? Description = null,
    bool IsActive = true,
    bool IsFeatured = false,
    int DisplayOrder = 0);
