using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Commands.CreateTag;

public sealed record CreateTagCommand(
    string Name,
    string? Url = null,
    bool IsActive = true) : IRequest<TagDto>;
