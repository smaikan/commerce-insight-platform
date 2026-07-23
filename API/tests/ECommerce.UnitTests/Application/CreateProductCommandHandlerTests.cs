using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class CreateProductCommandHandlerTests
{
    [Fact]
    public void Validator_Should_Reject_Product_Without_Variants()
    {
        var result = new CreateProductCommandValidator().Validate(new CreateProductCommand("Product"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateProductCommand.Variants) &&
            error.ErrorMessage == "A product must have at least one variant.");
    }

    [Fact]
    public async Task Handle_Should_Create_Product_With_Generated_Url_When_Url_Is_Not_Provided()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var productUrlGenerator = new ProductUrlGenerator();
        var typeId = Guid.NewGuid();
        Product? createdProduct = null;

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        productRepository
            .Setup(repository => repository.UrlExistsAsync("basic-t-shirt", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productRepository
            .Setup(repository => repository.GetExistingVariantSkusAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => createdProduct = product.WithId(1))
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            collectionRepository.Object,
            productUrlGenerator,
            unitOfWork.Object);

        var result = await handler.Handle(new CreateProductCommand(
            "Basic T-Shirt",
            typeId,
            Variants: [new CreateProductVariantItem("Standard", "TSHIRT-STD", 100, 10)]), CancellationToken.None);

        result.Title.Should().Be("Basic T-Shirt");
        result.Url.Should().Be("basic-t-shirt");
        result.TypeId.Should().Be(typeId);
        result.Variants.Should().ContainSingle(variant =>
            variant.Name == "Standard" && variant.Sku == "TSHIRT-STD");
        createdProduct.Should().NotBeNull();
        createdProduct!.Url.Should().Be("basic-t-shirt");
        createdProduct.Variants.Should().ContainSingle(variant => variant.Name == "Standard");
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFoundException_When_Product_Type_Does_Not_Exist()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var typeId = Guid.NewGuid();

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            collectionRepository.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(new CreateProductCommand(
            "Basic T-Shirt",
            typeId,
            Variants: [new CreateProductVariantItem("Standard", "TSHIRT-STD", 100, 10)]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Create_Product_Without_Type_And_With_Collections()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var collectionId = Guid.NewGuid();
        Product? createdProduct = null;

        collectionRepository
            .Setup(repository => repository.GetExistingIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { collectionId });

        productRepository
            .Setup(repository => repository.UrlExistsAsync("type-free-product", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productRepository
            .Setup(repository => repository.GetExistingVariantSkusAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => createdProduct = product.WithId(1))
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            collectionRepository.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateProductCommand(
                "Type Free Product",
                CollectionIds: [collectionId],
                Variants: [new CreateProductVariantItem("Standard", "TYPE-FREE-STD", 100, 0)]),
            CancellationToken.None);

        result.TypeId.Should().BeNull();
        createdProduct.Should().NotBeNull();
        createdProduct!.ProductCollections.Should().ContainSingle(relation => relation.CollectionId == collectionId);
        createdProduct.Variants.Should().ContainSingle();
        productTypeRepository.Verify(
            repository => repository.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
