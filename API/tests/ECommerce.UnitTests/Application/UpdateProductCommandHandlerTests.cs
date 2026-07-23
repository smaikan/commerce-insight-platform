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
    [Fact]
    public async Task Handle_Should_Update_Product_Basics_Type_And_Brand()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentTypeId = Guid.NewGuid();
        var newTypeId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var product = new Product("Old Product", "old-product", currentTypeId).WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.UrlExistsAsync("new-product", product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(newTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        brandRepository
            .Setup(repository => repository.ExistsAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductCommand(
                product.Id,
                "New Product",
                newTypeId,
                BrandId: brandId,
                Description: "Updated",
                DisplayOrder: 3),
            CancellationToken.None);

        result.Title.Should().Be("New Product");
        result.Url.Should().Be("new-product");
        result.TypeId.Should().Be(newTypeId);
        result.BrandId.Should().Be(brandId);
        result.DisplayOrder.Should().Be(3);
        product.Description.Should().Be("Updated");
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_ConflictException_When_Url_Already_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var typeId = Guid.NewGuid();
        var product = new Product("Old Product", "old-product", typeId).WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        productRepository
            .Setup(repository => repository.UrlExistsAsync("taken-product", product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new UpdateProductCommand(product.Id, "Taken Product", typeId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
