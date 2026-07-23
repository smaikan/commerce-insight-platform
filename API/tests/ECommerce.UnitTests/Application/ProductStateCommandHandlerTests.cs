using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Commands.ChangeProductStatus;
using ECommerce.Application.Products.Commands.SetProductActivation;
using ECommerce.Application.Products.Commands.SetProductFeatured;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductStateCommandHandlerTests
{
    [Fact]
    public async Task ChangeProductStatus_Should_Update_Status()
    {
        var productRepository = new Mock<IProductRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", Guid.NewGuid()).WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ChangeProductStatusCommandHandler(productRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new ChangeProductStatusCommand(product.Id, ProductStatus.Active),
            CancellationToken.None);

        result.Status.Should().Be(ProductStatus.Active);
        product.Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public async Task SetProductActivation_Should_Deactivate_Product()
    {
        var productRepository = new Mock<IProductRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", Guid.NewGuid(), isActive: true).WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SetProductActivationCommandHandler(productRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(new SetProductActivationCommand(product.Id, false), CancellationToken.None);

        result.IsActive.Should().BeFalse();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SetProductFeatured_Should_Mark_Product_As_Featured()
    {
        var productRepository = new Mock<IProductRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", Guid.NewGuid(), isFeatured: false).WithId(1);

        productRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SetProductFeaturedCommandHandler(productRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(new SetProductFeaturedCommand(product.Id, true), CancellationToken.None);

        result.IsFeatured.Should().BeTrue();
        product.IsFeatured.Should().BeTrue();
    }
}
