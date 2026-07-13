using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Commands.UpdateCollection;

public sealed record UpdateCollectionCommand(
    Guid Id,
    string Name,
    string? Url = null,
    string? Description = null,
    int DisplayOrder = 0) : IRequest<CollectionDto>;
