using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Commands.BulkCreateTags;

public sealed record BulkCreateTagsCommand(
    IReadOnlyList<BulkCreateTagItem> Tags) : IRequest<IReadOnlyList<TagDto>>;

public sealed record BulkCreateTagItem(
    string Name,
    string? Url = null,
    bool IsActive = true);
