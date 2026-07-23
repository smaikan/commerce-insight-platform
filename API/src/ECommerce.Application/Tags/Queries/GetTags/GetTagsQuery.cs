using ECommerce.Application.Tags.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Tags.Queries.GetTags;

public sealed record GetTagsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<TagDto>>;
