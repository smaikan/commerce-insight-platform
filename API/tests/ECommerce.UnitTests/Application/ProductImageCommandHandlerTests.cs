using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Products.Images.Commands.CreateProductImage;
using ECommerce.Application.Products.Images.Commands.UpdateProductImage;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductImageCommandHandlerTests
{
    // Burada mevcut ürüne yeni görsel eklendiğini doğruluyorum.
    [Fact]
    public async Task CreateProductImage_Should_Create_Image_When_Product_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        var imageRepository = new Mock<IProductImageRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var product = new Product("Product", "product", "PRODUCT-MAIN", Guid.NewGuid()).WithId(1);
        var currentMainImage = new ProductImage(product.Id, "https://cdn.test/old-main.jpg", 0, true);
        ProductImage? createdImage = null;

        productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        imageRepository
            .Setup(repository => repository.GetMainByProductIdForUpdateAsync(product.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentMainImage);

        imageRepository
            .Setup(repository => repository.AddAsync(It.IsAny<ProductImage>(), It.IsAny<CancellationToken>()))
            .Callback<ProductImage, CancellationToken>((image, _) => createdImage = image)
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateProductImageCommandHandler(
            productRepository.Object,
            imageRepository.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateProductImageCommand(product.Id, "https://cdn.test/product.jpg", "Front", 1, true),
            CancellationToken.None);

        result.ProductId.Should().Be(PublicIdCodec.EncodeProductId(product.Id));
        result.ImageUrl.Should().Be("https://cdn.test/product.jpg");
        result.AltText.Should().Be("Front");
        result.DisplayOrder.Should().Be(1);
        result.IsMain.Should().BeTrue();
        createdImage.Should().NotBeNull();
        currentMainImage.IsMain.Should().BeFalse();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada bulunmayan ürüne görsel eklenmesini engelliyorum.
    [Fact]
    public async Task CreateProductImage_Should_Throw_NotFoundException_When_Product_Does_Not_Exist()
    {
        var productRepository = new Mock<IProductRepository>();
        var imageRepository = new Mock<IProductImageRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        const long productId = 1;

        productRepository
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = new CreateProductImageCommandHandler(
            productRepository.Object,
            imageRepository.Object,
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new CreateProductImageCommand(productId, "https://cdn.test/product.jpg"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        imageRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductImage>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada mevcut ürün görselinin temel alanlarının güncellendiğini doğruluyorum.
    [Fact]
    public async Task UpdateProductImage_Should_Update_Image()
    {
        var imageRepository = new Mock<IProductImageRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var image = new ProductImage(1, "https://cdn.test/old.jpg", 1, false, "Old");
        var currentMainImage = new ProductImage(image.ProductId, "https://cdn.test/main.jpg", 0, true);

        imageRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        imageRepository
            .Setup(repository => repository.GetMainByProductIdForUpdateAsync(image.ProductId, image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentMainImage);

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateProductImageCommandHandler(imageRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateProductImageCommand(image.Id, "https://cdn.test/new.jpg", "New", 2, true),
            CancellationToken.None);

        result.ImageUrl.Should().Be("https://cdn.test/new.jpg");
        result.AltText.Should().Be("New");
        result.DisplayOrder.Should().Be(2);
        result.IsMain.Should().BeTrue();
        image.ImageUrl.Should().Be("https://cdn.test/new.jpg");
        currentMainImage.IsMain.Should().BeFalse();
    }
}
