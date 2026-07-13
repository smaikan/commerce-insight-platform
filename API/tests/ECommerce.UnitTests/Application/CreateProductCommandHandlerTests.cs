using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_Product_With_Generated_Url_When_Url_Is_Not_Provided()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
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
            .Setup(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => createdProduct = product)
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            productUrlGenerator,
            unitOfWork.Object);

        var result = await handler.Handle(new CreateProductCommand("Basic T-Shirt", typeId), CancellationToken.None);

        result.Title.Should().Be("Basic T-Shirt");
        result.Url.Should().Be("basic-t-shirt");
        result.TypeId.Should().Be(typeId);
        createdProduct.Should().NotBeNull();
        createdProduct!.Url.Should().Be("basic-t-shirt");
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFoundException_When_Product_Type_Does_Not_Exist()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var typeId = Guid.NewGuid();

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateProductCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(new CreateProductCommand("Basic T-Shirt", typeId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
