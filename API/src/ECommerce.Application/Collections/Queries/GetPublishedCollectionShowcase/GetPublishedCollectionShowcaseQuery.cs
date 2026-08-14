using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetPublishedCollectionShowcase;

// Burada public koleksiyon vitrininin sayfalı sorgu sözleşmesini tanımlıyorum.
public sealed record GetPublishedCollectionShowcaseQuery(
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<PublishedCollectionShowcaseItemDto>>;
