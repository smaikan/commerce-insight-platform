using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Variants.Commands.CreateProductVariant;
using ECommerce.Application.Products.Variants.Commands.SetProductVariantActivation;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariantPrice;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductVariantCommandHandlerTests
{
    [Fact]
    public async Task CreateProductVariant_Should_Create_Variant_When_Product_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", Guid.NewGuid());
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
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateProductVariantCommand(product.Id, "SKU-1", 100, 8, CompareAtPrice: 120, Color: "Black"),
            CancellationToken.None);

        result.ProductId.Should().Be(product.Id);
        result.Sku.Should().Be("SKU-1");
        result.Price.Should().Be(100);
        result.Stock.Should().Be(8);
        result.CompareAtPrice.Should().Be(120);
        result.Color.Should().Be("Black");
        createdVariant.Should().NotBeNull();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductVariant_Should_Throw_ConflictException_When_Sku_Already_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", Guid.NewGuid());

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        variantRepository
            .Setup(repository => repository.SkuExistsAsync("SKU-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateProductVariantCommandHandler(
            productRepository.Object,
            variantRepository.Object,
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new CreateProductVariantCommand(product.Id, "SKU-1", 100, 8),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductVariantPrice_Should_Update_Price()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-1", 100, 8);

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
        result.CompareAtPrice.Should().Be(110);
        variant.Price.Should().Be(90);
        variant.CompareAtPrice.Should().Be(110);
    }

    [Fact]
    public async Task UpdateProductVariant_Should_Update_Details_Price_Stock_And_Activation()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-1", 100, 8, isActive: true);

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
                "SKU-2",
                90,
                12,
                CompareAtPrice: 120,
                Barcode: "BAR-2",
                Color: "Black",
                Size: "M",
                Material: "Cotton",
                IsActive: false),
            CancellationToken.None);

        result.Sku.Should().Be("SKU-2");
        result.Price.Should().Be(90);
        result.Stock.Should().Be(12);
        result.CompareAtPrice.Should().Be(120);
        result.Barcode.Should().Be("BAR-2");
        result.Color.Should().Be("Black");
        result.Size.Should().Be("M");
        result.Material.Should().Be("Cotton");
        result.IsActive.Should().BeFalse();
        variant.Sku.Should().Be("SKU-2");
        variant.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductVariant_Should_Throw_ConflictException_When_Sku_Already_Exists()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-1", 100, 8);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        variantRepository
            .Setup(repository => repository.SkuExistsAsync("SKU-2", variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdateProductVariantCommandHandler(variantRepository.Object, unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new UpdateProductVariantCommand(variant.Id, "SKU-2", 90, 12),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductVariantStock_Should_Update_Stock()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-1", 100, 8);

        variantRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductVariantStockCommandHandler(variantRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductVariantStockCommand(variant.Id, 15),
            CancellationToken.None);

        result.Stock.Should().Be(15);
        variant.Stock.Should().Be(15);
    }

    [Fact]
    public async Task SetProductVariantActivation_Should_Deactivate_Variant()
    {
        var variantRepository = new Mock<IProductVariantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-1", 100, 8, isActive: true);

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
