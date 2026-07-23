using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.AddFavorite;

public sealed class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public AddFavoriteCommandHandler(IProductRepository products, IProductEngagementRepository engagement,
        ICurrentUserService currentUser, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _products = products;
        _engagement = engagement;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var product = await _products.GetByIdForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        if (await _engagement.GetFavoriteForUpdateAsync(product.Id, userId, cancellationToken) is not null)
        {
            throw new ConflictException("Product is already in favorites.");
        }

        await _engagement.AddFavoriteAsync(new FavoriteProduct(product.Id, userId), cancellationToken);
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
    }
}
