using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Commands.UpdateCollection;

// Burada koleksiyon güncelleme isteğinin alanlarını tanımlıyorum.
public sealed record UpdateCollectionCommand(
    Guid Id,
    string Name,
    string? Url = null,
    string? Description = null,
    int DisplayOrder = 0,
    string? ImageUrl = null) : IRequest<CollectionDto>;
