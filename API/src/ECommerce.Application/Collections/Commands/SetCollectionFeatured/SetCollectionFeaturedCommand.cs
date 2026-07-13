using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Commands.SetCollectionFeatured;

public sealed record SetCollectionFeaturedCommand(Guid Id, bool IsFeatured) : IRequest<CollectionDto>;
