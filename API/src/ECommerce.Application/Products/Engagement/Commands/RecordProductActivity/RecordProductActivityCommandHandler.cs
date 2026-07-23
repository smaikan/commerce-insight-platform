using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;

public sealed class RecordProductActivityCommandHandler : IRequestHandler<RecordProductActivityCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductVariantRepository _variants;
    private readonly IProductEngagementRepository _engagement;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün hareketlerini işlemek için gerekli bağımlılıkları hazırlıyorum.
    public RecordProductActivityCommandHandler(IProductRepository products, IProductVariantRepository variants,
        IProductEngagementRepository engagement, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _products = products; _variants = variants; _engagement = engagement; _clock = clock; _unitOfWork = unitOfWork;
    }

    // Burada müşterinin tıklama veya sepete ekleme hareketini ürün metriklerine yansıtıyorum.
    public async Task Handle(RecordProductActivityCommand request, CancellationToken cancellationToken)
    {
        if (request.ActivityType == ProductActivityType.Purchase)
        {
            throw new ConflictException("Purchase activity must be recorded by the trusted order workflow.");
        }

        var product = await _products.GetByIdForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = await _variants.GetByIdForUpdateAsync(request.ProductVariantId.Value, cancellationToken)
                ?? throw new NotFoundException("Product variant was not found.");
            if (variant.ProductId != product.Id)
            {
                throw new ConflictException("Product variant does not belong to the product.");
            }
        }

        var date = DateOnly.FromDateTime(_clock.UtcNow);
        var productMetric = await _engagement.GetProductDailyMetricForUpdateAsync(product.Id, date, cancellationToken);
        if (productMetric is null)
        {
            productMetric = new ProductDailyMetric(product.Id, date);
            await _engagement.AddProductDailyMetricAsync(productMetric, cancellationToken);
        }

        ProductVariantDailyMetric? variantMetric = null;
        if (variant is not null)
        {
            variantMetric = await _engagement.GetVariantDailyMetricForUpdateAsync(variant.Id, date, cancellationToken);
            if (variantMetric is null)
            {
                variantMetric = new ProductVariantDailyMetric(variant.Id, date);
                await _engagement.AddVariantDailyMetricAsync(variantMetric, cancellationToken);
            }
        }

        switch (request.ActivityType)
        {
            case ProductActivityType.Click:
                product.IncreaseClickCount();
                productMetric.IncreaseClickCount();
                break;
            case ProductActivityType.AddToCart:
                product.IncreaseTotalAddToCartCount(request.Quantity);
                productMetric.IncreaseAddToCartCount(request.Quantity);
                variant!.IncreaseAddToCartCount(request.Quantity);
                variantMetric!.IncreaseAddToCartCount(request.Quantity);
                break;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
