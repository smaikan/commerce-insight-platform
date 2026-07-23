using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductReviews;

public sealed record GetProductReviewsQuery(long ProductId, int PageNumber = 1, int PageSize = 20, bool ApprovedOnly = true)
    : IRequest<PagedResult<ProductReviewDto>>;
