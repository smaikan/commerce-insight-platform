using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Accounting.CostLayers;

public sealed class OpeningBalanceCostLayerWriterTests
{
    // Burada writer'ın pozitif açılış stoklu varyant için tek sıfır maliyet katmanı hazırladığını doğruluyorum.
    [Fact]
    public async Task Writer_Should_Add_One_Layer_For_Positive_Opening_Stock()
    {
        var repository = new Mock<IOpeningBalanceCostLayerRepository>();
        var costRepository = new Mock<IInventoryCostRepository>();
        var variant = CreateVariant(8);
        InventoryCostLayer? addedLayer = null;
        ProductVariantCostHistory? addedHistory = null;
        repository
            .Setup(item => item.GetExistingStockMovementIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());
        repository
            .Setup(item => item.Add(It.IsAny<InventoryCostLayer>()))
            .Callback<InventoryCostLayer>(layer => addedLayer = layer);
        costRepository
            .Setup(item => item.AddHistoryAsync(
                It.IsAny<ProductVariantCostHistory>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProductVariantCostHistory, CancellationToken>(
                (history, _) => addedHistory = history)
            .Returns(Task.CompletedTask);
        var writer = new OpeningBalanceCostLayerWriter(
            repository.Object,
            costRepository.Object);

        await writer.CreateForNewVariantsAsync(
            [variant],
            CancellationToken.None);

        addedLayer.Should().NotBeNull();
        addedLayer!.SourceType.Should().Be(
            InventoryCostLayerSourceType.OpeningBalance);
        addedLayer.RemainingQuantity.Should().Be(8);
        addedLayer.UnitCostExcludingVat.Should().Be(0m);
        addedHistory.Should().NotBeNull();
        addedHistory!.SourceType.Should().Be(
            ProductVariantCostHistorySourceType.OpeningBalance);
        addedHistory.SourceId.Should().Be(addedLayer.Id);
        repository.Verify(
            item => item.Add(It.IsAny<InventoryCostLayer>()),
            Times.Once);
    }

    // Burada KDV dahil maliyet verilmediğinde writer'ın KDV hariç maliyeti iki alana da aktardığını doğruluyorum.
    [Fact]
    public async Task Writer_Should_Use_Excluding_Cost_When_Including_Cost_Is_Omitted()
    {
        var repository = new Mock<IOpeningBalanceCostLayerRepository>();
        var costRepository = new Mock<IInventoryCostRepository>();
        var variant = CreateVariant(4);
        InventoryCostLayer? addedLayer = null;
        repository
            .Setup(item => item.GetExistingStockMovementIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());
        repository
            .Setup(item => item.Add(It.IsAny<InventoryCostLayer>()))
            .Callback<InventoryCostLayer>(layer => addedLayer = layer);
        var writer = new OpeningBalanceCostLayerWriter(
            repository.Object,
            costRepository.Object);

        await writer.CreateForNewVariantsAsync(
            [new OpeningBalanceCostLayerSeed(variant, 12.3456m)],
            CancellationToken.None);

        addedLayer.Should().NotBeNull();
        addedLayer!.UnitCostExcludingVat.Should().Be(12.3456m);
        addedLayer.UnitCostIncludingVat.Should().Be(12.3456m);
        addedLayer.TotalCostExcludingVat.Should().Be(49.38m);
        addedLayer.TotalCostIncludingVat.Should().Be(49.38m);
    }

    // Burada sıfır stoklu varyanta pozitif maliyetin writer doğrudan çağrılsa da sessizce atılmadığını doğruluyorum.
    [Fact]
    public async Task Writer_Should_Reject_Positive_Cost_For_Zero_Opening_Stock()
    {
        var repository = new Mock<IOpeningBalanceCostLayerRepository>();
        var writer = new OpeningBalanceCostLayerWriter(
            repository.Object,
            Mock.Of<IInventoryCostRepository>());

        Func<Task> act = () => writer.CreateForNewVariantsAsync(
            [new OpeningBalanceCostLayerSeed(CreateVariant(0), 10m)],
            CancellationToken.None);

        await act.Should().ThrowAsync<
            ECommerce.Domain.Common.DomainException>();
        repository.Verify(
            item => item.Add(It.IsAny<InventoryCostLayer>()),
            Times.Never);
    }

    // Burada sıfır açılış stoklu varyant için maliyet katmanı üretilmediğini doğruluyorum.
    [Fact]
    public async Task Writer_Should_Not_Add_Layer_For_Zero_Opening_Stock()
    {
        var repository = new Mock<IOpeningBalanceCostLayerRepository>();
        var costRepository = new Mock<IInventoryCostRepository>();
        repository
            .Setup(item => item.GetExistingStockMovementIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());
        var writer = new OpeningBalanceCostLayerWriter(
            repository.Object,
            costRepository.Object);

        await writer.CreateForNewVariantsAsync(
            [CreateVariant(0)],
            CancellationToken.None);

        repository.Verify(
            item => item.Add(It.IsAny<InventoryCostLayer>()),
            Times.Never);
    }

    // Burada daha önce katmanı bulunan OpeningBalance hareketi için writer tekrarının ikinci katman üretmediğini doğruluyorum.
    [Fact]
    public async Task Writer_Should_Be_Idempotent_For_Existing_Opening_Layer()
    {
        var repository = new Mock<IOpeningBalanceCostLayerRepository>();
        var costRepository = new Mock<IInventoryCostRepository>();
        var variant = CreateVariant(3);
        var movementId = variant.StockMovements.Single().Id;
        repository
            .Setup(item => item.GetExistingStockMovementIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { movementId });
        var writer = new OpeningBalanceCostLayerWriter(
            repository.Object,
            costRepository.Object);

        await writer.CreateForNewVariantsAsync(
            [variant],
            CancellationToken.None);

        repository.Verify(
            item => item.Add(It.IsAny<InventoryCostLayer>()),
            Times.Never);
        costRepository.Verify(
            item => item.AddHistoryAsync(
                It.IsAny<ProductVariantCostHistory>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Burada writer testleri için belirtilen açılış stoğuyla gerçek varyant aggregate'ı oluşturuyorum.
    private static ProductVariant CreateVariant(int stock)
    {
        return new ProductVariant(
            1,
            "Default",
            $"WRITER-{Guid.NewGuid():N}",
            100m,
            stock,
            netPrice: 100m);
    }
}
