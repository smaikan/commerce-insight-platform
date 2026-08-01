using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Commands.UpdateProduct;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class UpdateProductCommandHandlerTests
{
    // Burada ürünün temel alanlarıyla birlikte ana SKU, tür, marka ve etiketlerinin güncellendiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Update_Product_Basics_Type_And_Brand()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var taxRateRepository = new Mock<ITaxRateRepository>();
        var productTagResolver = new Mock<IProductTagResolver>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentTypeId = Guid.NewGuid();
        var newTypeId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var product = new Product("Old Product", "old-product", "OLD-MAIN", currentTypeId).WithId(1);

        productRepository
            .Setup(repository => repository.GetWithRelationsForUpdateAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.UrlExistsAsync("new-product", product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productRepository
            .Setup(repository => repository.MainSkuExistsAsync(
                "NEW-MAIN", product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(newTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        brandRepository
            .Setup(repository => repository.ExistsAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        productTagResolver
            .Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductTagResolution(
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["New Season"] = tagId
                }));

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            taxRateRepository.Object,
            productTagResolver.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductCommand(
                product.Id,
                "New Product",
                "new-main",
                newTypeId,
                BrandId: brandId,
                Description: "Updated",
                DisplayOrder: 3,
                Tags: ["New Season"]),
            CancellationToken.None);

        result.Title.Should().Be("New Product");
        result.MainSku.Should().Be("NEW-MAIN");
        result.Url.Should().Be("new-product");
        result.TypeId.Should().Be(newTypeId);
        result.BrandId.Should().Be(brandId);
        result.DisplayOrder.Should().Be(3);
        product.Description.Should().Be("Updated");
        product.ProductTags.Should().ContainSingle(tag => tag.TagId == tagId);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada boş etiket listesi gönderildiğinde ürünün mevcut etiketlerinin temizlendiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Clear_Product_Tags_When_Tags_Are_Empty()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTagResolver = new Mock<IProductTagResolver>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", "MAIN-SKU").WithId(1);
        product.ProductTags.Add(new ProductTag(product.Id, Guid.NewGuid()));

        productRepository
            .Setup(repository => repository.GetWithRelationsForUpdateAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.MainSkuExistsAsync(
                "MAIN-SKU",
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productRepository
            .Setup(repository => repository.UrlExistsAsync(
                "product",
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductCommandHandler(
            productRepository.Object,
            Mock.Of<IProductTypeRepository>(),
            Mock.Of<IBrandRepository>(),
            Mock.Of<ITaxRateRepository>(),
            productTagResolver.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        await handler.Handle(
            new UpdateProductCommand(
                product.Id,
                "Product",
                "MAIN-SKU",
                TypeId: null,
                Url: "product",
                Tags: []),
            CancellationToken.None);

        product.ProductTags.Should().BeEmpty();
        productTagResolver.VerifyNoOtherCalls();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada başka üründe kullanılan URL ile ürün güncellenmesini engelliyorum.
    [Fact]
    public async Task Handle_Should_Throw_ConflictException_When_Url_Already_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var taxRateRepository = new Mock<ITaxRateRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var typeId = Guid.NewGuid();
        var product = new Product("Old Product", "old-product", "OLD-MAIN", typeId).WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        productRepository
            .Setup(repository => repository.UrlExistsAsync("taken-product", product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        productRepository
            .Setup(repository => repository.MainSkuExistsAsync(
                "TAKEN-MAIN", product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new UpdateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            taxRateRepository.Object,
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new UpdateProductCommand(product.Id, "Taken Product", "TAKEN-MAIN", typeId, "taken-product"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada başka üründe kullanılan ana SKU ile ürün güncellenmesini engelliyorum.
    [Fact]
    public async Task Handle_Should_Throw_ConflictException_When_Main_Sku_Already_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var product = new Product("Old Product", "old-product", "OLD-MAIN").WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.MainSkuExistsAsync(
                "TAKEN-MAIN", product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateProductCommandHandler(
            productRepository.Object,
            Mock.Of<IProductTypeRepository>(),
            Mock.Of<IBrandRepository>(),
            Mock.Of<ITaxRateRepository>(),
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new UpdateProductCommand(product.Id, "Product", "taken-main", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        product.MainSku.Should().Be("OLD-MAIN");
        unitOfWork.Verify(
            unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
