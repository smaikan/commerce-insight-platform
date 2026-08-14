using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Engagement.Services;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetFavoriteProducts;

public sealed class GetFavoriteProductsQueryHandler : IRequestHandler<GetFavoriteProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductEngagementRepository _repository;
    private readonly IFavoriteOwnerResolver _ownerResolver;

    // Burada favori listesini güvenli owner kapsamında okumak için bağımlılıkları hazırlıyorum.
    public GetFavoriteProductsQueryHandler(
        IProductEngagementRepository repository,
        IFavoriteOwnerResolver ownerResolver)
    {
        _repository = repository;
        _ownerResolver = ownerResolver;
    }

    // Burada kullanıcı veya guest session favorilerini aynı ürün DTO sözleşmesiyle sayfalıyorum.
    public async Task<PagedResult<ProductDto>> Handle(GetFavoriteProductsQuery request, CancellationToken cancellationToken) =>
        (await _repository.GetFavoriteProductsAsync(
            _ownerResolver.Resolve(request.SessionId),
            request.PageNumber,
            request.PageSize,
            cancellationToken))
            .Map(product => product.ToDto());
}
