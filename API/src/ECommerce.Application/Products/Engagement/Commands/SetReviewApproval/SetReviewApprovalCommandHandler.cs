using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.SetReviewApproval;

public sealed class SetReviewApprovalCommandHandler : IRequestHandler<SetReviewApprovalCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public SetReviewApprovalCommandHandler(IProductRepository products, IProductEngagementRepository engagement,
        IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _products = products; _engagement = engagement; _clock = clock; _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetReviewApprovalCommand request, CancellationToken cancellationToken)
    {
        var review = await _engagement.GetReviewForUpdateAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("Product review was not found.");
        if (review.IsApproved == request.IsApproved)
        {
            return;
        }
        var product = await _products.GetByIdForUpdateAsync(review.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        if (request.IsApproved)
        {
            review.Approve();
            product.IncreaseReviewCount();
            var date = DateOnly.FromDateTime(_clock.UtcNow);
            var metric = await _engagement.GetProductDailyMetricForUpdateAsync(product.Id, date, cancellationToken);
            if (metric is null)
            {
                metric = new ProductDailyMetric(product.Id, date);
                await _engagement.AddProductDailyMetricAsync(metric, cancellationToken);
            }
            metric.IncreaseReviewCount();
        }
        else
        {
            review.Reject();
            product.DecreaseReviewCount();
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
