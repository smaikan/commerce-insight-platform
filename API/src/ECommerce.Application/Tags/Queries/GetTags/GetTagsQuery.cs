using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Queries.GetTags;

public sealed record GetTagsQuery : IRequest<IReadOnlyList<TagDto>>;
