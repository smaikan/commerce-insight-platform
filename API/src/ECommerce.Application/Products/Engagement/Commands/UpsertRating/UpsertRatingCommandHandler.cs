using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.UpsertRating;

public sealed class UpsertRatingCommandHandler : IRequestHandler<UpsertRatingCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün puanlama işlemi için gerekli bağımlılıkları hazırlıyorum.
    public UpsertRatingCommandHandler(IProductRepository products, IProductEngagementRepository engagement,
        ICurrentUserService currentUser, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _products = products; _engagement = engagement; _currentUser = currentUser; _clock = clock; _unitOfWork = unitOfWork;
    }

    // Burada yalnızca teslim edilmiş bir siparişte bulunan ürünün puanını ekliyor veya güncelliyorum.
    public async Task Handle(UpsertRatingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var product = await _products.GetByIdForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        if (!await _engagement.HasDeliveredPurchaseAsync(product.Id, userId, cancellationToken))
        {
            throw new ConflictException("The product can only be rated after it has been delivered.");
        }

        var rating = await _engagement.GetRatingForUpdateAsync(product.Id, userId, cancellationToken);
        var aggregate = await _engagement.GetRatingAggregateAsync(product.Id, rating?.Id, cancellationToken);
        var isNew = rating is null;
        if (rating is null)
        {
            rating = new ProductRating(product.Id, userId, request.RatingValue);
            await _engagement.AddRatingAsync(rating, cancellationToken);
        }
        else
        {
            rating.UpdateRatingValue(request.RatingValue);
        }

        var count = aggregate.Count + 1;
        product.UpdateRatingSummary((aggregate.Sum + request.RatingValue) / count, count);
        if (isNew)
        {
            var date = DateOnly.FromDateTime(_clock.UtcNow);
            var metric = await _engagement.GetProductDailyMetricForUpdateAsync(product.Id, date, cancellationToken);
            if (metric is null)
            {
                metric = new ProductDailyMetric(product.Id, date);
                await _engagement.AddProductDailyMetricAsync(metric, cancellationToken);
            }
            metric.IncreaseRatingCount();
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
