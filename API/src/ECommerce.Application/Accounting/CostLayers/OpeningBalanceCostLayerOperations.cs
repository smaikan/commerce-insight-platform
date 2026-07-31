using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Accounting.CostLayers;

// Burada OpeningBalance katmanının yalnız kalan miktarına uygulanacak maliyetleri ve beklenen eşzamanlılık anahtarını taşıyorum.
public sealed record UpdateOpeningBalanceCostLayerCommand(
    Guid Id,
    decimal UnitCostExcludingVat,
    decimal? UnitCostIncludingVat,
    Guid ExpectedConcurrencyToken) : IRequest<OpeningBalanceCostLayerDto>;

// Burada varyant kimliğiyle OpeningBalance maliyet katmanı detayını istemek için sorgu sözleşmesini taşıyorum.
public sealed record GetOpeningBalanceCostLayerByVariantQuery(
    Guid ProductVariantId) : IRequest<OpeningBalanceCostLayerDto>;

public sealed class UpdateOpeningBalanceCostLayerCommandValidator
    : AbstractValidator<UpdateOpeningBalanceCostLayerCommand>
{
    // Burada açılış maliyeti güncellemesinin kimlik, negatif olmayan maliyet ve eşzamanlılık alanlarını doğruluyorum.
    public UpdateOpeningBalanceCostLayerCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.UnitCostExcludingVat).GreaterThanOrEqualTo(0m);
        RuleFor(command => command.UnitCostIncludingVat)
            .GreaterThanOrEqualTo(0m)
            .When(command => command.UnitCostIncludingVat.HasValue);
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
    }
}

public sealed class GetOpeningBalanceCostLayerByVariantQueryValidator
    : AbstractValidator<GetOpeningBalanceCostLayerByVariantQuery>
{
    // Burada açılış maliyet katmanı sorgusunun varyant kimliğini doğruluyorum.
    public GetOpeningBalanceCostLayerByVariantQueryValidator()
    {
        RuleFor(query => query.ProductVariantId).NotEmpty();
    }
}

public sealed class OpeningBalanceCostLayerWriter : IOpeningBalanceCostLayerWriter
{
    private readonly IOpeningBalanceCostLayerRepository _repository;
    private readonly IInventoryCostRepository _costRepository;

    // Burada açılış katmanı ile maliyet geçmişini aynı persistence takibine ekleyecek repository'leri hazırlıyorum.
    public OpeningBalanceCostLayerWriter(
        IOpeningBalanceCostLayerRepository repository,
        IInventoryCostRepository costRepository)
    {
        _repository = repository;
        _costRepository = costRepository;
    }

    // Burada maliyet gönderilmeyen eski çağrıları sıfır maliyetli seed listesine dönüştürüyorum.
    public async Task CreateForNewVariantsAsync(
        IEnumerable<ProductVariant> variants,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variants);
        await CreateForNewVariantsAsync(
            variants.Select(variant => new OpeningBalanceCostLayerSeed(variant)),
            cancellationToken);
    }

    // Burada varyant ve maliyet eşlerini tek OpeningBalance hareketi başına idempotent katmanlara dönüştürüyorum.
    public async Task CreateForNewVariantsAsync(
        IEnumerable<OpeningBalanceCostLayerSeed> seeds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seeds);
        var orderedSeeds = seeds
            .GroupBy(seed => seed.Variant.Id)
            .Select(group => group.First())
            .OrderBy(seed => seed.Variant.Id)
            .ToArray();
        var openingMovements =
            new List<(OpeningBalanceCostLayerSeed Seed, StockMovement Movement)>();

        foreach (var seed in orderedSeeds)
        {
            ArgumentNullException.ThrowIfNull(seed.Variant);
            var variant = seed.Variant;
            var unitCostExcludingVat =
                seed.OpeningUnitCostExcludingVat ?? 0m;
            // Burada KDV dahil maliyet gönderilmezse stok değerlemenin ana KDV hariç maliyetini aynen kullanıyorum.
            var unitCostIncludingVat =
                seed.OpeningUnitCostIncludingVat ?? unitCostExcludingVat;
            if (unitCostExcludingVat < 0m || unitCostIncludingVat < 0m)
            {
                throw new DomainException(
                    "Opening unit costs cannot be negative.");
            }

            if (variant.Stock == 0)
            {
                if (unitCostExcludingVat > 0m || unitCostIncludingVat > 0m)
                {
                    throw new DomainException(
                        "A positive opening unit cost requires positive opening stock.");
                }

                continue;
            }

            var variantOpeningMovements = variant.StockMovements
                .Where(movement =>
                    movement.Type == StockMovementType.OpeningBalance)
                .ToArray();
            if (variantOpeningMovements.Length != 1)
            {
                throw new DomainException(
                    "A positive new product variant must contain exactly one OpeningBalance movement.");
            }

            openingMovements.Add((seed, variantOpeningMovements[0]));
        }

        var existingMovementIds =
            await _repository.GetExistingStockMovementIdsAsync(
                openingMovements.Select(item => item.Movement.Id),
                cancellationToken);
        foreach (var (seed, movement) in openingMovements)
        {
            if (existingMovementIds.Contains(movement.Id))
            {
                continue;
            }

            var layer = new InventoryCostLayer(
                seed.Variant,
                movement,
                seed.OpeningUnitCostExcludingVat ?? 0m,
                seed.OpeningUnitCostIncludingVat);
            var previous =
                await _costRepository.GetActiveHistoryForUpdateAsync(
                    seed.Variant.Id,
                    cancellationToken);
            previous?.Close(layer.CostDate, seed.Variant.Stock);
            await _costRepository.AddHistoryAsync(
                new ProductVariantCostHistory(
                    seed.Variant.Id,
                    previous?.NewCostExcludingVat,
                    layer.UnitCostExcludingVat,
                    previous?.NewCostIncludingVat,
                    layer.UnitCostIncludingVat,
                    layer.CostDate,
                    seed.Variant.Stock,
                    layer.Id,
                    ProductVariantCostHistorySourceType.OpeningBalance),
                cancellationToken);
            _repository.Add(layer);
        }
    }
}

public sealed class OpeningBalanceCostLayerHandlers :
    IRequestHandler<UpdateOpeningBalanceCostLayerCommand, OpeningBalanceCostLayerDto>,
    IRequestHandler<GetOpeningBalanceCostLayerByVariantQuery, OpeningBalanceCostLayerDto>
{
    private readonly IOpeningBalanceCostLayerRepository _repository;
    private readonly IInventoryCostRepository _costRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada OpeningBalance katmanı, maliyet geçmişi ve UoW bağımlılıklarını hazırlıyorum.
    public OpeningBalanceCostLayerHandlers(
        IOpeningBalanceCostLayerRepository repository,
        IInventoryCostRepository costRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _costRepository = costRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada yalnız tüketilmemiş OpeningBalance miktarının gelecekteki tüketim maliyetini eşzamanlı olarak güncelliyorum.
    public async Task<OpeningBalanceCostLayerDto> Handle(
        UpdateOpeningBalanceCostLayerCommand request,
        CancellationToken cancellationToken)
    {
        var layer = await _repository.GetByIdForUpdateAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                "Opening balance cost layer was not found.");
        if (layer.ConcurrencyToken != request.ExpectedConcurrencyToken)
        {
            throw new ConcurrencyException(
                "The opening balance cost layer was changed by another operation.");
        }

        var previous = await _costRepository.GetActiveHistoryForUpdateAsync(
            layer.ProductVariantId,
            cancellationToken);
        var previousUnitCostExcludingVat =
            previous?.NewCostExcludingVat ?? layer.UnitCostExcludingVat;
        var previousUnitCostIncludingVat =
            previous?.NewCostIncludingVat ?? layer.UnitCostIncludingVat;
        var validFrom = DateTime.UtcNow;
        var resolvedUnitCostIncludingVat =
            request.UnitCostIncludingVat ?? request.UnitCostExcludingVat;
        layer.UpdateOpeningBalanceRemainingCost(
            request.UnitCostExcludingVat,
            resolvedUnitCostIncludingVat,
            request.ExpectedConcurrencyToken);
        previous?.Close(validFrom, layer.RemainingQuantity);
        await _costRepository.AddHistoryAsync(
            new ProductVariantCostHistory(
                layer.ProductVariantId,
                previousUnitCostExcludingVat,
                layer.UnitCostExcludingVat,
                previousUnitCostIncludingVat,
                layer.UnitCostIncludingVat,
                validFrom,
                layer.RemainingQuantity,
                layer.Id,
                ProductVariantCostHistorySourceType.OpeningBalance),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(layer);
    }

    // Burada varyantın OpeningBalance maliyet katmanını güncel miktar, maliyet ve token bilgileriyle getiriyorum.
    public async Task<OpeningBalanceCostLayerDto> Handle(
        GetOpeningBalanceCostLayerByVariantQuery request,
        CancellationToken cancellationToken)
    {
        var layer = await _repository.GetByProductVariantIdAsync(
            request.ProductVariantId,
            cancellationToken)
            ?? throw new NotFoundException(
                "Opening balance cost layer was not found.");
        return Map(layer);
    }

    // Burada domain katmanını dış sözleşmeye maliyet ve eşzamanlılık snapshot'ıyla dönüştürüyorum.
    private static OpeningBalanceCostLayerDto Map(InventoryCostLayer layer)
    {
        return new OpeningBalanceCostLayerDto(
            layer.Id,
            layer.ProductVariantId,
            layer.StockMovementId,
            layer.SourceType,
            layer.OriginalQuantity,
            layer.RemainingQuantity,
            layer.UnitCostExcludingVat,
            layer.UnitCostIncludingVat,
            layer.TotalCostExcludingVat,
            layer.TotalCostIncludingVat,
            layer.CostDate,
            layer.Status,
            layer.ConcurrencyToken);
    }
}
