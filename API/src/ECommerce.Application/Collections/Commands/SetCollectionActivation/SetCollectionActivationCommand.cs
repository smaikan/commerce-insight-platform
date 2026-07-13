using ECommerce.Application.Collections.Dtos;
using MediatR;

namespace ECommerce.Application.Collections.Commands.SetCollectionActivation;

public sealed record SetCollectionActivationCommand(Guid Id, bool IsActive) : IRequest<CollectionDto>;
