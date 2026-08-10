using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Commands.BulkCreateCollections;

// Burada toplu koleksiyon oluşturma isteğini tanımlıyorum.
public sealed record BulkCreateCollectionsCommand(
    IReadOnlyList<BulkCreateCollectionItem> Collections) : IRequest<IReadOnlyList<CollectionDto>>;

// Burada toplu istekteki tek koleksiyon kaydının alanlarını tanımlıyorum.
public sealed record BulkCreateCollectionItem(
    string Name,
    string? Url = null,
    string? Description = null,
    bool IsActive = true,
    bool IsFeatured = false,
    int DisplayOrder = 0,
    string? ImageUrl = null);
