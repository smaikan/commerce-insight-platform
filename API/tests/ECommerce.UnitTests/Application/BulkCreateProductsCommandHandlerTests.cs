using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Commands.BulkCreateProducts;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace ECommerce.UnitTests.Application;

public sealed class BulkCreateProductsCommandHandlerTests
{
    // Burada Swagger gövdesindeki varyantın tek JSON kurucusuna bağlandığını doğruluyorum.
    [Fact]
    public void Bulk_Request_Should_Deserialize_Variant_Item()
    {
        const string requestBody = """
            {
              "products": [
                {
                  "title": "Luna İnce Halka Küpe",
                  "mainSku": "AUR-EAR-001",
                  "hasVariants": false,
                  "variants": [
                    {
                      "name": "Standart",
                      "value": "Tek seçenek",
                      "sku": "AUR-EAR-001-STD",
                      "price": 499.90,
                      "stock": 10
                    }
                  ]
                }
              ]
            }
            """;

        var command = JsonSerializer.Deserialize<BulkCreateProductsCommand>(
            requestBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        command.Should().NotBeNull();
        command!.Products[0].Variants![0].Value.Should().Be("Tek seçenek");
    }

    // Burada toplu ürünlerin ana SKU, varyant, görsel ve ilişkileriyle oluşturulduğunu doğruluyorum.
    [Fact]
    public async Task Handle_Should_Create_Products_With_Variants_Images_Collections_And_Tags()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var taxRateRepository = new Mock<ITaxRateRepository>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var tagRepository = new Mock<ITagRepository>();
        var productTagResolver = new Mock<IProductTagResolver>();
        var openingBalanceCostLayerWriter =
            new Mock<IOpeningBalanceCostLayerWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var typeId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var dynamicTagId = Guid.NewGuid();
        IReadOnlyCollection<Product>? createdProducts = null;

        productRepository
            .Setup(repository => repository.GetExistingUrlsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productRepository
            .Setup(repository => repository.GetExistingVariantSkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productRepository
            .Setup(repository => repository.GetExistingMainSkusAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productRepository
            .Setup(repository => repository.AddRangeAsync(It.IsAny<IReadOnlyCollection<Product>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Product>, CancellationToken>((products, _) =>
            {
                var id = 1L;
                foreach (var product in products)
                {
                    product.WithId(id++);
                }

                createdProducts = products;
            })
            .Returns(Task.CompletedTask);

        productRepository
            .Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
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

        productTagResolver
            .Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductTagResolution(
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["New Season"] = dynamicTagId
                }));

        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new BulkCreateProductsCommandHandler(
            productRepository.Object,
            productTypeRepository.Object,
            brandRepository.Object,
            taxRateRepository.Object,
            collectionRepository.Object,
            productTagResolver.Object,
            new ProductUrlGenerator(),
            openingBalanceCostLayerWriter.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new BulkCreateProductsCommand(
            [
                new BulkCreateProductItem(
                    "Premium Hoodie",
                    "hoodie-main",
                    Type: "Hoodie",
                    BrandId: brandId,
                    Variants:
                    [
                        new BulkCreateProductVariantItem(
                            "Black / Medium",
                            "HOODIE-BLK-M",
                            1299.90m,
                            25,
                            OpeningUnitCostExcludingVat: 700.25m,
                            OpeningUnitCostIncludingVat: 840.30m)
                    ],
                    Images:
                    [
                        new BulkCreateProductImageItem("https://cdn.example.com/hoodie.jpg", IsMain: true)
                    ],
                    Collections: ["Summer Collection"],
                    Tags: ["New Season"])
            ]),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].MainSku.Should().Be("HOODIE-MAIN");
        result[0].Url.Should().Be("premium-hoodie");
        createdProducts.Should().ContainSingle();
        createdProducts!.Single().Variants.Should().ContainSingle();
        createdProducts.Single().Images.Should().ContainSingle();
        createdProducts.Single().ProductCollections.Should().ContainSingle();
        createdProducts.Single().ProductTags.Should().HaveCount(1);
        createdProducts.Single().ProductTags.Select(tag => tag.TagId)
            .Should().BeEquivalentTo([dynamicTagId]);
        openingBalanceCostLayerWriter.Verify(
            writer => writer.CreateForNewVariantsAsync(
                It.Is<IEnumerable<OpeningBalanceCostLayerSeed>>(seeds =>
                    seeds.Single().Variant.Stock == 25 &&
                    seeds.Single().OpeningUnitCostExcludingVat == 700.25m &&
                    seeds.Single().OpeningUnitCostIncludingVat == 840.30m),
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada toplu istekte ana SKU tekrarını büyük-küçük harften bağımsız yakalıyorum.
    [Fact]
    public async Task Handle_Should_Reject_Duplicate_Main_Skus_In_Request()
    {
        var handler = new BulkCreateProductsCommandHandler(
            Mock.Of<IProductRepository>(),
            Mock.Of<IProductTypeRepository>(),
            Mock.Of<IBrandRepository>(),
            Mock.Of<ITaxRateRepository>(),
            Mock.Of<ICollectionRepository>(),
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            Mock.Of<IOpeningBalanceCostLayerWriter>(),
            Mock.Of<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new BulkCreateProductsCommand(
            [
                new BulkCreateProductItem(
                    "Product One",
                    "main-sku",
                    Variants: [new BulkCreateProductVariantItem("Standard", "ONE-STD", 100m, 1)]),
                new BulkCreateProductItem(
                    "Product Two",
                    "MAIN-SKU",
                    Variants: [new BulkCreateProductVariantItem("Standard", "TWO-STD", 100m, 1)])
            ]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
    }

    // Burada veritabanında kullanılan ana SKU ile toplu ürün oluşturulmasını engelliyorum.
    [Fact]
    public async Task Handle_Should_Reject_Main_Sku_That_Already_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(repository => repository.GetExistingMainSkusAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { "TAKEN-MAIN" });

        var handler = new BulkCreateProductsCommandHandler(
            productRepository.Object,
            Mock.Of<IProductTypeRepository>(),
            Mock.Of<IBrandRepository>(),
            Mock.Of<ITaxRateRepository>(),
            Mock.Of<ICollectionRepository>(),
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            Mock.Of<IOpeningBalanceCostLayerWriter>(),
            Mock.Of<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new BulkCreateProductsCommand(
            [
                new BulkCreateProductItem(
                    "Product",
                    "taken-main",
                    Variants: [new BulkCreateProductVariantItem("Standard", "PRODUCT-STD", 100m, 1)])
            ]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        productRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Product>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Burada toplu ürün isteğinde sıfır stokla gönderilen pozitif açılış maliyetini reddediyorum.
    [Fact]
    public void Validator_Should_Reject_Positive_Opening_Cost_Without_Stock()
    {
        var result = new BulkCreateProductsCommandValidator().Validate(
            new BulkCreateProductsCommand(
            [
                new BulkCreateProductItem(
                    "Product",
                    "PRODUCT-MAIN",
                    Variants:
                    [
                        new BulkCreateProductVariantItem(
                            "Standard",
                            "PRODUCT-STD",
                            100m,
                            0,
                            OpeningUnitCostIncludingVat: 12m)
                    ])
            ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName.EndsWith(
                nameof(BulkCreateProductVariantItem.OpeningUnitCostIncludingVat),
                StringComparison.Ordinal));
    }
}
