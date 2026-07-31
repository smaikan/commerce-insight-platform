using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Products.Variants.Commands.CreateProductVariant;
using ECommerce.Application.Products.Variants.Commands.DeleteProductVariant;
using ECommerce.Application.Products.Variants.Commands.SetProductVariantActivation;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariantPrice;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductVariantCommandHandlerTests
{
    // Burada yeni varyantın negatif, aşırı hassas veya stoksuz pozitif açılış maliyetini reddediyorum.
    [Theory]
    [InlineData(-1, 1, false)]
    [InlineData(10.12345, 1, false)]
    [InlineData(10, 0, false)]
    [InlineData(0, 0, true)]
    public void CreateProductVariantValidator_Should_Validate_Opening_Cost(
        decimal openingUnitCost,
        int stock,
        bool expectedValidity)
    {
        var result = new CreateProductVariantCommandValidator().Validate(
            new CreateProductVariantCommand(
                1,
                "Standard",
                "SKU-OPENING",
                100m,
                stock,
                OpeningUnitCostExcludingVat: openingUnitCost));

        result.IsValid.Should().Be(expectedValidity);
    }

    // Burada ürünün son varyantının silinmesini engelliyorum.
    [Fact]
    public async Task DeleteProductVariant_Should_Reject_Deleting_Last_Variant()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var productRepository = new Mock<IProductRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-1", 100, 8);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        variantRepository
            .Setup(repository => repository.CountByProductIdAsync(variant.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new DeleteProductVariantCommandHandler(
            variantRepository.Object,
            productRepository.Object,
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(new DeleteProductVariantCommand(variant.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("A product must have at least one variant.");
        variantRepository.Verify(repository => repository.Remove(It.IsAny<ProductVariant>()), Times.Never);
    }

    // Burada stok hareketi audit geçmişi bulunan varyantın fiziksel olarak silinmesini engelliyorum.
    [Fact]
    public async Task DeleteProductVariant_Should_Reject_Variant_With_Stock_Movement_History()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var productRepository = new Mock<IProductRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-AUDIT", 100m, 1);
        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(
                variant.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        variantRepository
            .Setup(repository => repository.CountByProductIdAsync(
                variant.ProductId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        variantRepository
            .Setup(repository => repository.HasStockMovementsAsync(
                variant.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new DeleteProductVariantCommandHandler(
            variantRepository.Object,
            productRepository.Object,
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new DeleteProductVariantCommand(variant.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*stock movement history*");
        variantRepository.Verify(
            repository => repository.Remove(It.IsAny<ProductVariant>()),
            Times.Never);
        unitOfWork.Verify(
            item => item.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Burada mevcut ürüne yeni varyant oluşturulduğunu doğruluyorum.
    [Fact]
    public async Task CreateProductVariant_Should_Create_Variant_When_Product_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var variantRepository = new Mock<IProductVariantRepository>();
        var openingBalanceCostLayerWriter =
            new Mock<IOpeningBalanceCostLayerWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        ProductVariant? createdVariant = null;

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        variantRepository
            .Setup(repository => repository.SkuExistsAsync("SKU-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        variantRepository
            .Setup(repository => repository.AddAsync(It.IsAny<ProductVariant>(), It.IsAny<CancellationToken>()))
            .Callback<ProductVariant, CancellationToken>((variant, _) => createdVariant = variant)
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateProductVariantCommandHandler(
            productRepository.Object,
            variantRepository.Object,
            openingBalanceCostLayerWriter.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateProductVariantCommand(
                product.Id,
                "Black / Medium",
                "SKU-1",
                100,
                8,
                CompareAtPrice: 120,
                OpeningUnitCostExcludingVat: 45.5m),
            CancellationToken.None);

        result.ProductId.Should().Be(PublicIdCodec.EncodeProductId(product.Id));
        result.Name.Should().Be("Black / Medium");
        result.Sku.Should().Be("SKU-1");
        result.Price.Should().Be(100);
        result.Stock.Should().Be(8);
        result.CompareAtPrice.Should().Be(120);
        createdVariant.Should().NotBeNull();
        createdVariant!.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.OpeningBalance &&
            movement.Direction == StockMovementDirection.In &&
            movement.QuantityDelta == 8 &&
            movement.StockBeforeMovement == 0 &&
            movement.StockAfterMovement == 8);
        openingBalanceCostLayerWriter.Verify(
            writer => writer.CreateForNewVariantsAsync(
                It.Is<IEnumerable<OpeningBalanceCostLayerSeed>>(seeds =>
                    seeds.Single().Variant == createdVariant &&
                    seeds.Single().OpeningUnitCostExcludingVat == 45.5m &&
                    seeds.Single().OpeningUnitCostIncludingVat == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada kullanılan varyant SKU değeriyle yeni varyant oluşturulmasını engelliyorum.
    [Fact]
    public async Task CreateProductVariant_Should_Throw_ConflictException_When_Sku_Already_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        variantRepository
            .Setup(repository => repository.SkuExistsAsync("SKU-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateProductVariantCommandHandler(
            productRepository.Object,
            variantRepository.Object,
            Mock.Of<IOpeningBalanceCostLayerWriter>(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new CreateProductVariantCommand(product.Id, "Standard", "SKU-1", 100, 8),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada varyant fiyat güncelleme komutunun fiyatı değiştirdiğini doğruluyorum.
    [Fact]
    public async Task UpdateProductVariantPrice_Should_Update_Price()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-1", 100, 8);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductVariantPriceCommandHandler(variantRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductVariantPriceCommand(variant.Id, 90, 110),
            CancellationToken.None);

        result.Price.Should().Be(90);
        result.NetPrice.Should().Be(90);
        result.CompareAtPrice.Should().Be(110);
        variant.Price.Should().Be(90);
        variant.CompareAtPrice.Should().Be(110);
    }

    // Burada varyant detay, fiyat, stok ve aktivasyon bilgilerinin birlikte güncellendiğini doğruluyorum.
    [Fact]
    public async Task UpdateProductVariant_Should_Update_Details_Price_Stock_And_Activation()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-1", 100, 8, isActive: true);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        variantRepository
            .Setup(repository => repository.SkuExistsAsync("SKU-2", variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductVariantCommandHandler(variantRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductVariantCommand(
                variant.Id,
                "Premium",
                "SKU-2",
                90,
                12,
                CompareAtPrice: 120,
                Barcode: "BAR-2",
                Material: "Cotton",
                IsActive: false,
                StockAdjustmentReason: "Warehouse count"),
            CancellationToken.None);

        result.Name.Should().Be("Premium");
        result.Sku.Should().Be("SKU-2");
        result.Price.Should().Be(90);
        result.NetPrice.Should().Be(90);
        result.Stock.Should().Be(12);
        result.CompareAtPrice.Should().Be(120);
        result.Barcode.Should().Be("BAR-2");
        result.Material.Should().Be("Cotton");
        result.IsActive.Should().BeFalse();
        variant.Sku.Should().Be("SKU-2");
        variant.IsActive.Should().BeFalse();
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.StockCountAdjustment &&
            movement.Direction == StockMovementDirection.In &&
            movement.QuantityDelta == 4 &&
            movement.StockBeforeMovement == 8 &&
            movement.StockAfterMovement == 12 &&
            movement.Reason == "Warehouse count");
    }

    // Burada başka varyantta kullanılan SKU ile varyant güncellenmesini engelliyorum.
    [Fact]
    public async Task UpdateProductVariant_Should_Throw_ConflictException_When_Sku_Already_Exists()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-1", 100, 8);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        variantRepository
            .Setup(repository => repository.SkuExistsAsync("SKU-2", variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdateProductVariantCommandHandler(variantRepository.Object, unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new UpdateProductVariantCommand(variant.Id, "Premium", "SKU-2", 90, 12),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada varyant stok güncelleme komutunun stoku artırdığını doğruluyorum.
    [Fact]
    public async Task UpdateProductVariantStock_Should_Update_Stock()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-1", 100, 8);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductVariantStockCommandHandler(variantRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductVariantStockCommand(
                variant.Id,
                7,
                StockMovementType.ManualAdjustment,
                "Manual correction"),
            CancellationToken.None);

        result.Stock.Should().Be(15);
        variant.Stock.Should().Be(15);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.ManualAdjustment &&
            movement.Direction == StockMovementDirection.In &&
            movement.QuantityDelta == 7 &&
            movement.StockBeforeMovement == 8 &&
            movement.StockAfterMovement == 15 &&
            movement.Reason == "Manual correction");
    }

    // Burada negatif stok değişiminin mevcut stoku azalttığını doğruluyorum.
    [Fact]
    public async Task UpdateProductVariantStock_Should_Decrease_Stock_When_Quantity_Is_Negative()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-1", 100, 8);
        variantRepository.Setup(repository => repository.GetByIdForUpdateAsync(
                variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new UpdateProductVariantStockCommandHandler(variantRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductVariantStockCommand(
                variant.Id,
                -2,
                StockMovementType.Damage,
                "Damaged item"),
            CancellationToken.None);

        result.Stock.Should().Be(6);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Damage &&
            movement.Direction == StockMovementDirection.Out &&
            movement.QuantityDelta == -2 &&
            movement.StockBeforeMovement == 8 &&
            movement.StockAfterMovement == 6 &&
            movement.Reason == "Damaged item");
    }

    // Burada varyant aktivasyon komutunun varyantı satışa kapattığını doğruluyorum.
    [Fact]
    public async Task SetProductVariantActivation_Should_Deactivate_Variant()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(1, "Standard", "SKU-1", 100, 8, isActive: true);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SetProductVariantActivationCommandHandler(variantRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new SetProductVariantActivationCommand(variant.Id, false),
            CancellationToken.None);

        result.IsActive.Should().BeFalse();
        variant.IsActive.Should().BeFalse();
    }
}
