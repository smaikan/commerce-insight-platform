using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetCollectionById;

public sealed record GetCollectionByIdQuery(Guid Id) : IRequest<CollectionDto>;
