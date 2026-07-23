using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductReviews;

public sealed class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, PagedResult<ProductReviewDto>>
{
    private readonly IProductEngagementRepository _repository;
    public GetProductReviewsQueryHandler(IProductEngagementRepository repository) => _repository = repository;
    public async Task<PagedResult<ProductReviewDto>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken) =>
        (await _repository.GetReviewsAsync(request.ProductId, request.ApprovedOnly, request.PageNumber, request.PageSize, cancellationToken))
            .Map(review => review.ToDto());
}
