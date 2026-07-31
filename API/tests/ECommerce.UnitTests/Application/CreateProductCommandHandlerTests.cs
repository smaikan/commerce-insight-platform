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
    // Burada varyantsız ürün oluşturma isteğinin doğrulamadan geçmediğini kontrol ediyorum.
    [Fact]
    public void Validator_Should_Reject_Product_Without_Variants()
    {
        var result = new CreateProductCommandValidator().Validate(
            new CreateProductCommand("Product", "PRODUCT-MAIN"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateProductCommand.Variants) &&
            error.ErrorMessage == "A product must have at least one variant.");
    }

    // Burada boş ana SKU ile ürün oluşturulmasını validator seviyesinde engelliyorum.
    [Fact]
    public void Validator_Should_Reject_Empty_Main_Sku()
    {
        var result = new CreateProductCommandValidator().Validate(
            new CreateProductCommand(
                "Product",
                " ",
                Variants: [new CreateProductVariantItem("Standard", "PRODUCT-STD", 100m, 1)]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateProductCommand.MainSku));
    }

    // Burada tek ürüne izin verilenden fazla etiket girilmesini validator seviyesinde engelliyorum.
    [Fact]
    public void Validator_Should_Reject_Too_Many_Tags()
    {
        var tags = Enumerable
            .Range(1, ProductTagRules.MaximumTagsPerProduct + 1)
            .Select(index => $"Tag {index}")
            .ToList();

        var result = new CreateProductCommandValidator().Validate(
            new CreateProductCommand(
                "Product",
                "PRODUCT-MAIN",
                Variants: [new CreateProductVariantItem("Standard", "PRODUCT-STD", 100m, 1)],
                Tags: tags));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateProductCommand.Tags));
    }

    // Burada ürünün ana SKU, aktif vergi oranı, oluşturulan URL ve adla çözümlenen etiketiyle kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Create_Product_With_Generated_Url_When_Url_Is_Not_Provided()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var taxRateRepository = new Mock<ITaxRateRepository>();
        var collectionRepository = new Mock<ICollectionRepository>();
        var productTagResolver = new Mock<IProductTagResolver>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var productUrlGenerator = new ProductUrlGenerator();
        var typeId = Guid.NewGuid();
        var taxRateId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        Product? createdProduct = null;

        productTypeRepository
            .Setup(repository => repository.ExistsAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var taxRate = new TaxRate("KDV", 20m);
        taxRateRepository
            .Setup(repository => repository.GetByIdAsync(taxRateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxRate);

        productRepository
            .Setup(repository => repository.UrlExistsAsync("basic-t-shirt", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productRepository
            .Setup(repository => repository.MainSkuExistsAsync(
                "TSHIRT-MAIN", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        productRepository
            .Setup(repository => repository.GetExistingVariantSkusAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        productTagResolver
            .Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductTagResolution(
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Summer"] = tagId
                }));

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
            taxRateRepository.Object,
            collectionRepository.Object,
            productTagResolver.Object,
            productUrlGenerator,
            unitOfWork.Object);

        var result = await handler.Handle(new CreateProductCommand(
            "Basic T-Shirt",
            "  tshirt-main  ",
            typeId,
            Variants: [new CreateProductVariantItem("Standard", "TSHIRT-STD", 100, 10)],
            Tags: [" Summer "],
            TaxRateId: taxRateId), CancellationToken.None);

        result.Title.Should().Be("Basic T-Shirt");
        result.MainSku.Should().Be("TSHIRT-MAIN");
        result.Url.Should().Be("basic-t-shirt");
        result.TypeId.Should().Be(typeId);
        result.TaxRateId.Should().Be(taxRateId);
        result.Variants.Should().ContainSingle(variant =>
            variant.Name == "Standard" && variant.Sku == "TSHIRT-STD");
        createdProduct.Should().NotBeNull();
        createdProduct!.MainSku.Should().Be("TSHIRT-MAIN");
        createdProduct.TaxRateId.Should().Be(taxRateId);
        createdProduct.Url.Should().Be("basic-t-shirt");
        createdProduct.Variants.Should().ContainSingle(variant => variant.Name == "Standard");
        createdProduct.Variants.Single().NetPrice.Should().Be(83.33m);
        createdProduct.ProductTags.Should().ContainSingle(tag => tag.TagId == tagId);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada bulunmayan ürün türüyle ürün oluşturulmasını engelliyorum.
    [Fact]
    public async Task Handle_Should_Throw_NotFoundException_When_Product_Type_Does_Not_Exist()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var taxRateRepository = new Mock<ITaxRateRepository>();
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
            taxRateRepository.Object,
            collectionRepository.Object,
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(new CreateProductCommand(
            "Basic T-Shirt",
            "TSHIRT-MAIN",
            typeId,
            Variants: [new CreateProductVariantItem("Standard", "TSHIRT-STD", 100, 10)]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada türü olmayan ürünün koleksiyon ve varyant ilişkileriyle oluşturulduğunu doğruluyorum.
    [Fact]
    public async Task Handle_Should_Create_Product_Without_Type_And_With_Collections()
    {
        var productRepository = new Mock<IProductRepository>();
        var productTypeRepository = new Mock<IProductTypeRepository>();
        var brandRepository = new Mock<IBrandRepository>();
        var taxRateRepository = new Mock<ITaxRateRepository>();
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
            taxRateRepository.Object,
            collectionRepository.Object,
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateProductCommand(
                "Type Free Product",
                "TYPE-FREE-MAIN",
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

    // Burada veritabanında kullanılan ana SKU ile ikinci ürün oluşturulmasını engelliyorum.
    [Fact]
    public async Task Handle_Should_Throw_ConflictException_When_Main_Sku_Already_Exists()
    {
        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(repository => repository.MainSkuExistsAsync(
                "TAKEN-MAIN", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateProductCommandHandler(
            productRepository.Object,
            Mock.Of<IProductTypeRepository>(),
            Mock.Of<IBrandRepository>(),
            Mock.Of<ITaxRateRepository>(),
            Mock.Of<ICollectionRepository>(),
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            Mock.Of<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new CreateProductCommand(
                "Product",
                "taken-main",
                Variants: [new CreateProductVariantItem("Standard", "PRODUCT-STD", 100m, 1)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        productRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Burada pasif veya bulunmayan vergi oranının yeni ürüne atanmasını kaydetmeden engelliyorum.
    [Fact]
    public async Task Handle_Should_Reject_Inactive_Or_Missing_TaxRate()
    {
        var productRepository = new Mock<IProductRepository>();
        var taxRateRepository = new Mock<ITaxRateRepository>();
        var taxRateId = Guid.NewGuid();
        productRepository
            .Setup(repository => repository.MainSkuExistsAsync(
                "PRODUCT-MAIN", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        taxRateRepository
            .Setup(repository => repository.GetByIdAsync(taxRateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxRate?)null);
        var handler = new CreateProductCommandHandler(
            productRepository.Object,
            Mock.Of<IProductTypeRepository>(),
            Mock.Of<IBrandRepository>(),
            taxRateRepository.Object,
            Mock.Of<ICollectionRepository>(),
            Mock.Of<IProductTagResolver>(),
            new ProductUrlGenerator(),
            Mock.Of<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new CreateProductCommand(
                "Product",
                "PRODUCT-MAIN",
                Variants: [new CreateProductVariantItem("Standard", "PRODUCT-STD", 100m, 1)],
                TaxRateId: taxRateId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        productRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
