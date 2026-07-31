using ECommerce.Domain.Accounting.CostLayers;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Accounting.CostLayers;

// Burada tek varyant maliyet geçmişi kaydının kaynak, maliyet, geçerlilik ve stok snapshot alanlarını taşıyorum.
public sealed record ProductVariantCostHistoryDto(
    Guid Id,
    Guid ProductVariantId,
    ProductVariantCostHistorySourceType SourceType,
    Guid SourceId,
    decimal? PreviousCostExcludingVat,
    decimal NewCostExcludingVat,
    decimal? PreviousCostIncludingVat,
    decimal NewCostIncludingVat,
    DateTime ValidFrom,
    DateTime? ValidTo,
    int OpeningStockQuantity,
    int? ClosingStockQuantity,
    DateTime CreatedAt);

// Burada seçili varyantın maliyet geçmişini kronolojik ve deterministik sırada istemeyi tanımlıyorum.
public sealed record GetProductVariantCostHistoryQuery(
    Guid ProductVariantId) : IRequest<IReadOnlyList<ProductVariantCostHistoryDto>>;

public interface IProductVariantCostHistoryReadRepository
{
    // Burada varyant maliyet geçmişini geçerlilik tarihi, oluşturulma tarihi ve kimlik sırasıyla okuma sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductVariantCostHistory>> GetByProductVariantIdAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}

public sealed class GetProductVariantCostHistoryQueryValidator
    : AbstractValidator<GetProductVariantCostHistoryQuery>
{
    // Burada maliyet geçmişi sorgusunun boş olmayan bir varyant kimliği taşımasını doğruluyorum.
    public GetProductVariantCostHistoryQueryValidator()
    {
        RuleFor(query => query.ProductVariantId).NotEmpty();
    }
}

public sealed class GetProductVariantCostHistoryQueryHandler
    : IRequestHandler<
        GetProductVariantCostHistoryQuery,
        IReadOnlyList<ProductVariantCostHistoryDto>>
{
    private readonly IProductVariantCostHistoryReadRepository _repository;

    // Burada maliyet geçmişi sorgusunu salt okunur repository ile hazırlıyorum.
    public GetProductVariantCostHistoryQueryHandler(
        IProductVariantCostHistoryReadRepository repository)
    {
        _repository = repository;
    }

    // Burada repository'nin deterministik kronolojik sonucunu API DTO kayıtlarına dönüştürüyorum.
    public async Task<IReadOnlyList<ProductVariantCostHistoryDto>> Handle(
        GetProductVariantCostHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var history = await _repository.GetByProductVariantIdAsync(
            request.ProductVariantId,
            cancellationToken);
        return history.Select(Map).ToArray();
    }

    // Burada maliyet geçmişi entity'sini bütün kaynak ve dönem alanlarıyla dış sözleşmeye dönüştürüyorum.
    private static ProductVariantCostHistoryDto Map(
        ProductVariantCostHistory history)
    {
        return new ProductVariantCostHistoryDto(
            history.Id,
            history.ProductVariantId,
            history.SourceType,
            history.SourceId,
            history.PreviousCostExcludingVat,
            history.NewCostExcludingVat,
            history.PreviousCostIncludingVat,
            history.NewCostIncludingVat,
            history.ValidFrom,
            history.ValidTo,
            history.OpeningStockQuantity,
            history.ClosingStockQuantity,
            history.CreatedAt);
    }
}
