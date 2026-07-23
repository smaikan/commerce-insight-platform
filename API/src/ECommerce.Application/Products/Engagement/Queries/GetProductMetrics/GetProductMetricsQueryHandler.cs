using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;

public sealed class GetProductMetricsQueryHandler : IRequestHandler<GetProductMetricsQuery, IReadOnlyList<ProductMetricDto>>
{
    private readonly IProductEngagementRepository _repository;
    public GetProductMetricsQueryHandler(IProductEngagementRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<ProductMetricDto>> Handle(GetProductMetricsQuery request, CancellationToken cancellationToken) =>
        (await _repository.GetProductMetricsAsync(request.ProductId, request.From, request.To, cancellationToken))
            .Select(metric => metric.ToDto()).ToList();
}
