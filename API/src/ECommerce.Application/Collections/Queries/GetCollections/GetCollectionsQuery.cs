using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetCollections;

public sealed record GetCollectionsQuery : IRequest<IReadOnlyList<CollectionDto>>;
