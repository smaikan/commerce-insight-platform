using ECommerce.Application.Common.Models;
using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Queries.GetPublishedProductTypeShowcase;

// Burada public kategori vitrininin sayfalı sorgu sözleşmesini tanımlıyorum.
public sealed record GetPublishedProductTypeShowcaseQuery(
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<PublishedProductTypeShowcaseItemDto>>;
