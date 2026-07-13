using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Commands.UpdateTag;

public sealed record UpdateTagCommand(
    Guid Id,
    string Name,
    string? Url = null) : IRequest<TagDto>;
