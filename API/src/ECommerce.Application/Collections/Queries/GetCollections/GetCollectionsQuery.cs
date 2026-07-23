using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetCollections;

public sealed record GetCollectionsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<CollectionDto>>;
