using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Commands.CreateCollection;

// Burada koleksiyon oluşturma isteğinin alanlarını tanımlıyorum.
public sealed record CreateCollectionCommand(
    string Name,
    string? Url = null,
    string? Description = null,
    bool IsActive = true,
    bool IsFeatured = false,
    int DisplayOrder = 0,
    string? ImageUrl = null) : IRequest<CollectionDto>;
