using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Commands.SetTagActivation;

public sealed record SetTagActivationCommand(Guid Id, bool IsActive) : IRequest<TagDto>;
