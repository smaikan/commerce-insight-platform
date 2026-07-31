using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Common;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Accounting.CostLayers;

public sealed class ProductVariantCostHistoryTests
{
    // Burada maliyet geçmişinin kaynak türünü, kaynak kimliğini ve birim maliyet hassasiyetini koruduğunu doğruluyorum.
    [Theory]
    [InlineData(ProductVariantCostHistorySourceType.PurchaseInvoice)]
    [InlineData(ProductVariantCostHistorySourceType.OpeningBalance)]
    public void Constructor_Should_Preserve_Source_And_Round_Costs(
        ProductVariantCostHistorySourceType sourceType)
    {
        var sourceId = Guid.NewGuid();

        var history = new ProductVariantCostHistory(
            Guid.NewGuid(),
            10.12345m,
            12.34567m,
            12.14815m,
            14.81481m,
            new DateTime(2026, 7, 26),
            8,
            sourceId,
            sourceType);

        history.SourceType.Should().Be(sourceType);
        history.SourceId.Should().Be(sourceId);
        history.PreviousCostExcludingVat.Should().Be(10.1235m);
        history.NewCostExcludingVat.Should().Be(12.3457m);
        history.PreviousCostIncludingVat.Should().Be(12.1482m);
        history.NewCostIncludingVat.Should().Be(14.8148m);
    }

    // Burada tanımsız kaynak türünün ve negatif geçmiş maliyetlerin domain tarafından reddedildiğini doğruluyorum.
    [Fact]
    public void Constructor_Should_Reject_Invalid_Source_And_Negative_Cost()
    {
        var invalidSource = () => new ProductVariantCostHistory(
            Guid.NewGuid(),
            null,
            10m,
            null,
            12m,
            new DateTime(2026, 7, 26),
            1,
            Guid.NewGuid(),
            (ProductVariantCostHistorySourceType)999);
        var negativePreviousCost = () => new ProductVariantCostHistory(
            Guid.NewGuid(),
            -1m,
            10m,
            null,
            12m,
            new DateTime(2026, 7, 26),
            1,
            Guid.NewGuid(),
            ProductVariantCostHistorySourceType.PurchaseInvoice);

        invalidSource.Should().Throw<DomainException>();
        negativePreviousCost.Should().Throw<DomainException>();
    }

    // Burada sorgu handler'ının repository sonucundaki bütün kaynak ve dönem alanlarını DTO'ya aktardığını doğruluyorum.
    [Fact]
    public async Task Query_Handler_Should_Map_Complete_History()
    {
        var productVariantId = Guid.NewGuid();
        var history = new ProductVariantCostHistory(
            productVariantId,
            5m,
            7m,
            6m,
            8.4m,
            new DateTime(2026, 7, 26),
            4,
            Guid.NewGuid(),
            ProductVariantCostHistorySourceType.PurchaseInvoice);
        history.Close(new DateTime(2026, 7, 27), 3);
        var repository = new Mock<IProductVariantCostHistoryReadRepository>();
        repository
            .Setup(item => item.GetByProductVariantIdAsync(
                productVariantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([history]);
        var handler = new GetProductVariantCostHistoryQueryHandler(
            repository.Object);

        var result = await handler.Handle(
            new GetProductVariantCostHistoryQuery(productVariantId),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(history.Id);
        result[0].ProductVariantId.Should().Be(productVariantId);
        result[0].SourceType.Should().Be(
            ProductVariantCostHistorySourceType.PurchaseInvoice);
        result[0].SourceId.Should().Be(history.SourceId);
        result[0].ValidFrom.Should().Be(history.ValidFrom);
        result[0].ValidTo.Should().Be(history.ValidTo);
        result[0].OpeningStockQuantity.Should().Be(4);
        result[0].ClosingStockQuantity.Should().Be(3);
    }

    // Burada varyant maliyet geçmişi sorgusunun boş kimliği kabul etmediğini doğruluyorum.
    [Fact]
    public void Query_Validator_Should_Require_ProductVariantId()
    {
        var validator = new GetProductVariantCostHistoryQueryValidator();

        var invalid = validator.Validate(
            new GetProductVariantCostHistoryQuery(Guid.Empty));
        var valid = validator.Validate(
            new GetProductVariantCostHistoryQuery(Guid.NewGuid()));

        invalid.IsValid.Should().BeFalse();
        valid.IsValid.Should().BeTrue();
    }
}
