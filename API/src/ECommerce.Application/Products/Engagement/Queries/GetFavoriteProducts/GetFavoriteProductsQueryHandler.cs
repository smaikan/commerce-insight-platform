using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetFavoriteProducts;

public sealed class GetFavoriteProductsQueryHandler : IRequestHandler<GetFavoriteProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductEngagementRepository _repository;
    private readonly ICurrentUserService _currentUser;
    public GetFavoriteProductsQueryHandler(IProductEngagementRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository; _currentUser = currentUser;
    }
    public async Task<PagedResult<ProductDto>> Handle(GetFavoriteProductsQuery request, CancellationToken cancellationToken) =>
        (await _repository.GetFavoriteProductsAsync(_currentUser.GetRequiredUserId(), request.PageNumber, request.PageSize, cancellationToken))
            .Map(product => product.ToDto());
}
