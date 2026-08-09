using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;

public sealed class GetProductMetricsQueryHandler : IRequestHandler<GetProductMetricsQuery, IReadOnlyList<ProductMetricDto>>
{
    private readonly IProductEngagementRepository _repository;

    // Burada günlük metrikleri okuyacak depoyu hazırlıyorum.
    public GetProductMetricsQueryHandler(IProductEngagementRepository repository) => _repository = repository;

    // Burada istenen UTC günlerinin tamamını hareketsiz günler dahil döndürüyorum.
    public async Task<IReadOnlyList<ProductMetricDto>> Handle(GetProductMetricsQuery request, CancellationToken cancellationToken)
    {
        var metricsByDate = (await _repository.GetProductMetricsAsync(
                request.ProductId, request.From, request.To, cancellationToken))
            .ToDictionary(metric => metric.Date);
        var dayCount = request.To.DayNumber - request.From.DayNumber + 1;

        return Enumerable.Range(0, dayCount)
            .Select(offset => metricsByDate.TryGetValue(request.From.AddDays(offset), out var metric)
                ? metric.ToDto()
                : new ProductMetricDto(request.From.AddDays(offset), 0, 0, 0, 0, 0, 0))
            .ToList();
    }
}
