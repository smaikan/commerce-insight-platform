using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Products.Engagement.Services;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.AddFavorite;

public sealed class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly IFavoriteOwnerResolver _ownerResolver;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada favori ekleme için ürün, sahiplik, metrik ve kalıcılık bağımlılıklarını hazırlıyorum.
    public AddFavoriteCommandHandler(IProductRepository products, IProductEngagementRepository engagement,
        IFavoriteOwnerResolver ownerResolver, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _products = products;
        _engagement = engagement;
        _ownerResolver = ownerResolver;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcı veya guest session sahibine ilk favoriyi sayaçlarla atomik biçimde ekliyorum.
    public async Task Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => AddFavoriteAsync(request, transactionCancellationToken),
            cancellationToken);
    }

    // Burada favori kaydı, ürün sayacı ve günlük metriği aynı transaction içinde güncelliyorum.
    private async Task<bool> AddFavoriteAsync(
        AddFavoriteCommand request,
        CancellationToken cancellationToken)
    {
        var owner = _ownerResolver.Resolve(request.SessionId);
        // Burada claim akışıyla aynı kilit sırasını korumak için önce owner favori aralığını okuyorum.
        if (await _engagement.GetFavoriteForUpdateAsync(request.ProductId, owner, cancellationToken) is not null)
        {
            throw new ConflictException("Product is already in favorites.");
        }

        var product = await _products.GetByIdForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        var favorite = owner.UserId.HasValue
            ? new FavoriteProduct(product.Id, owner.UserId.Value)
            : new FavoriteProduct(product.Id, owner.SessionId!);
        await _engagement.AddFavoriteAsync(favorite, cancellationToken);
        product.IncreaseFavoriteCount();
        var date = DateOnly.FromDateTime(_clock.UtcNow);
        var metric = await _engagement.GetProductDailyMetricForUpdateAsync(product.Id, date, cancellationToken);
        if (metric is null)
        {
            metric = new ProductDailyMetric(product.Id, date);
            await _engagement.AddProductDailyMetricAsync(metric, cancellationToken);
        }
        metric.IncreaseFavoriteCount();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
