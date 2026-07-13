using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Commands.BulkCreateProducts;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class BulkCreateProductsCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_Products_With_Variants_Images_Collections_And_Tags()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var tagRepository = new Mock<ITagRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var typeId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        IReadOnlyCollection<Product>? createdProducts = null;

        productRepository
            .Setup(repository => repository.GetExistingUrlsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productRepository
            .Setup(repository => repository.GetExistingVariantSkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productRepository
            .Setup(repository => repository.AddRangeAsync(It.IsAny<IReadOnlyCollection<Product>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Product>, CancellationToken>((products, _) => createdProducts = products)
            .Returns(Task.CompletedTask);

        productRepository
            .Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => createdProducts!.ToList());

        productTypeRepository
            .Setup(repository => repository.GetExistingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { typeId });

        brandRepository
            .Setup(repository => repository.GetExistingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { brandId });

        collectionRepository
            .Setup(repository => repository.GetExistingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { collectionId });

        tagRepository
            .Setup(repository => repository.GetExistingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { tagId });

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new BulkCreateProductsCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            collectionRepository.Object,
            tagRepository.Object,
            new ProductUrlGenerator(),
            unitOfWork.Object);

        var result = await handler.Handle(
            new BulkCreateProductsCommand(
            [
                new BulkCreateProductItem(
                    "Premium Hoodie",
                    typeId,
                    BrandId: brandId,
                    Variants:
                    [
                        new BulkCreateProductVariantItem("HOODIE-BLK-M", 1299.90m, 25)
                    ],
                    Images:
                    [
                        new BulkCreateProductImageItem("https://cdn.example.com/hoodie.jpg", IsMain: true)
                    ],
                    CollectionIds: [collectionId],
                    TagIds: [tagId])
            ]),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Url.Should().Be("premium-hoodie");
        createdProducts.Should().ContainSingle();
        createdProducts!.Single().Variants.Should().ContainSingle();
        createdProducts.Single().Images.Should().ContainSingle();
        createdProducts.Single().ProductCollections.Should().ContainSingle();
        createdProducts.Single().ProductTags.Should().ContainSingle();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
