using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;

public sealed class RecordProductActivityCommandHandler : IRequestHandler<RecordProductActivityCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün hareketlerini işlemek için gerekli bağımlılıkları hazırlıyorum.
    public RecordProductActivityCommandHandler(
        IProductRepository products,
        IProductEngagementRepository engagement,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _products = products;
        _engagement = engagement;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    // Burada yalnız doğrudan kaydedilebilen tıklama hareketini ürün metriklerine yansıtıyorum.
    public async Task Handle(RecordProductActivityCommand request, CancellationToken cancellationToken)
    {
        if (request.ActivityType != ProductActivityType.Click)
        {
            throw new ConflictException(
                "Add-to-cart and purchase activities must be recorded by their trusted workflows.");
        }

        var product = await _products.GetByIdForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        var date = DateOnly.FromDateTime(_clock.UtcNow);
        var productMetric = await _engagement.GetProductDailyMetricForUpdateAsync(product.Id, date, cancellationToken);
        if (productMetric is null)
        {
            productMetric = new ProductDailyMetric(product.Id, date);
            await _engagement.AddProductDailyMetricAsync(productMetric, cancellationToken);
        }

        product.IncreaseClickCount();
        productMetric.IncreaseClickCount();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
