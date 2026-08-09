using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.ReplaceProductPerformanceMetrics;

public sealed class ReplaceProductPerformanceMetricsCommandHandler
    : IRequestHandler<ReplaceProductPerformanceMetricsCommand, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReplaceProductPerformanceMetricsCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(
        ReplaceProductPerformanceMetricsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            throw new ConflictException("At least one product performance metric is required.");
        }

        var requestedIds = request.Items.Select(item => item.ProductId).ToList();
        if (requestedIds.Any(id => id <= 0) || requestedIds.Distinct().Count() != requestedIds.Count)
        {
            throw new ConflictException("Product performance metrics must contain unique valid product ids.");
        }

        var products = await _productRepository.GetByIdsForUpdateAsync(requestedIds, cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);
        var missingIds = requestedIds.Where(id => !productsById.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new NotFoundException($"Products were not found: {string.Join(", ", missingIds)}.");
        }

        foreach (var item in request.Items)
        {
            productsById[item.ProductId].ReplacePerformanceMetrics(
                item.ClickCount,
                item.TotalAddToCartCount,
                item.TotalPurchaseCount,
                item.FavoriteCount,
                item.AverageRating,
                item.RatingCount,
                item.ReviewCount);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return request.Items.Select(item => productsById[item.ProductId].ToDto()).ToList();
    }
}
