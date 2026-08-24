using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Variants.Commands.BulkUpdateProductVariants;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class BulkUpdateProductVariantsCommandHandlerTests
{
    // Burada batch içindeki yinelenen varyant kimliğini ve hedef SKU değerini validation aşamasında reddediyorum.
    [Fact]
    public void Validator_Should_Reject_Duplicate_Variant_Ids_And_Target_Skus()
    {
        var id = Guid.NewGuid();
        var token = Guid.NewGuid();
        var command = new BulkUpdateProductVariantsCommand(1,
        [
            CreateItem(id, "SKU-A", token),
            CreateItem(id, "sku-a", token)
        ]);

        var result = new BulkUpdateProductVariantsCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("same product variant"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Target SKU values"));
    }

    // Burada geçersiz alanlar ve eşleşmeyen birleşik seçenek parçalarının 400 validation sözleşmesine girdiğini doğruluyorum.
    [Fact]
    public void Validator_Should_Reject_Invalid_Item_Fields()
    {
        var command = new BulkUpdateProductVariantsCommand(1,
        [
            new BulkUpdateProductVariantItem(
                Guid.Empty,
                "Renk / Beden",
                "Kırmızı",
                new string('S', 101),
                0,
                -1,
                Guid.Empty,
                CompareAtPrice: -1,
                Barcode: new string('B', 101),
                Material: new string('M', 121),
                StockAdjustmentReason: new string('R', 501))
        ]);

        var result = new BulkUpdateProductVariantsCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain([
            "Variants[0].Id",
            "Variants[0].Sku",
            "Variants[0].Price",
            "Variants[0].Stock",
            "Variants[0].ExpectedConcurrencyToken"
        ]);
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("matching unique parts"));
    }

    // Burada iki varyantın SKU değerlerini ara unique-index çakışması olmadan tek komutta değiştirdiğimi doğruluyorum.
    [Fact]
    public async Task Handler_Should_Swap_Two_Skus_And_Create_Stock_Movement_Once()
    {
        var product = new Product("Product", "product", "MAIN-SKU").WithId(1);
        var first = new ProductVariant(product, "Uzunluk", "SKU-A", 100m, 2, value: "45 CM");
        var second = new ProductVariant(product, "Uzunluk", "SKU-B", 100m, 3, value: "50 CM");
        var variantRepository = CreateRepositoryMock(first, second);
        var optionResolver = CreateOptionResolverMock();
        var unitOfWork = CreateExecutingUnitOfWorkMock();
        var handler = new BulkUpdateProductVariantsCommandHandler(
            variantRepository.Object,
            optionResolver.Object,
            unitOfWork.Object);
        var request = new BulkUpdateProductVariantsCommand(product.Id,
        [
            CreateItem(first.Id, "SKU-B", first.ConcurrencyToken, value: "45 CM", stock: 5),
            CreateItem(second.Id, "SKU-A", second.ConcurrencyToken, value: "50 CM", stock: 3)
        ]);

        var result = await handler.Handle(request, CancellationToken.None);

        result.Select(variant => variant.Sku).Should().Equal("SKU-B", "SKU-A");
        first.Sku.Should().Be("SKU-B");
        second.Sku.Should().Be("SKU-A");
        first.StockMovements.Should().ContainSingle(movement =>
            movement.QuantityDelta == 3 && movement.Reason == "Batch stock count");
        second.StockMovements.Should().NotContain(movement =>
            movement.Type == ECommerce.Domain.Enums.StockMovementType.StockCountAdjustment);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // Burada başka ürüne ait bir varyant kimliğinin bütün batch'i işlem başlamadan reddettiğini doğruluyorum.
    [Fact]
    public async Task Handler_Should_Reject_Variant_From_Another_Product()
    {
        var firstProduct = new Product("First", "first", "FIRST-MAIN").WithId(1);
        var secondProduct = new Product("Second", "second", "SECOND-MAIN").WithId(2);
        var first = new ProductVariant(firstProduct, "Standard", "SKU-A", 100m, 0);
        var second = new ProductVariant(secondProduct, "Standard", "SKU-B", 100m, 0);
        var repository = CreateRepositoryMock(first, second);
        var unitOfWork = CreateExecutingUnitOfWorkMock();
        var handler = new BulkUpdateProductVariantsCommandHandler(
            repository.Object,
            CreateOptionResolverMock().Object,
            unitOfWork.Object);
        var request = new BulkUpdateProductVariantsCommand(firstProduct.Id,
        [
            CreateItem(first.Id, "SKU-B", first.ConcurrencyToken),
            CreateItem(second.Id, "SKU-A", second.ConcurrencyToken)
        ]);

        var action = () => handler.Handle(request, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada batch dışındaki SKU sahibini typed alan hatasıyla bildirip hiçbir değişikliği kaydetmiyorum.
    [Fact]
    public async Task Handler_Should_Return_Typed_Conflict_For_Sku_Outside_Batch()
    {
        var product = new Product("Product", "product", "MAIN-SKU").WithId(1);
        var first = new ProductVariant(product, "Standard", "SKU-A", 100m, 0);
        var repository = CreateRepositoryMock(first);
        repository
            .Setup(item => item.GetExistingSkusAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["SKU-OUTSIDE"]);
        var unitOfWork = CreateExecutingUnitOfWorkMock();
        var handler = new BulkUpdateProductVariantsCommandHandler(
            repository.Object,
            CreateOptionResolverMock().Object,
            unitOfWork.Object);
        var request = new BulkUpdateProductVariantsCommand(product.Id,
        [
            CreateItem(first.Id, "SKU-OUTSIDE", first.ConcurrencyToken)
        ]);

        var action = () => handler.Handle(request, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ProductVariantSkuConflictException>();
        exception.Which.ErrorCode.Should().Be("product_variant_sku_conflict");
        exception.Which.Errors.Should().ContainKey("variants[0].sku");
        first.Sku.Should().Be("SKU-A");
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada eski concurrency token bulunan tek satırın bütün batch'i kayıttan önce durdurduğunu doğruluyorum.
    [Fact]
    public async Task Handler_Should_Reject_Stale_Concurrency_Token_Before_Any_Save()
    {
        var product = new Product("Product", "product", "MAIN-SKU").WithId(1);
        var first = new ProductVariant(product, "Standard", "SKU-A", 100m, 0);
        var repository = CreateRepositoryMock(first);
        var unitOfWork = CreateExecutingUnitOfWorkMock();
        var handler = new BulkUpdateProductVariantsCommandHandler(
            repository.Object,
            CreateOptionResolverMock().Object,
            unitOfWork.Object);
        var request = new BulkUpdateProductVariantsCommand(product.Id,
        [
            CreateItem(first.Id, "SKU-B", Guid.NewGuid())
        ]);

        var action = () => handler.Handle(request, CancellationToken.None);

        await action.Should().ThrowAsync<ConcurrencyException>();
        first.Sku.Should().Be("SKU-A");
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada test varyant satırını geçerli varsayılan değerlerle oluşturuyorum.
    private static BulkUpdateProductVariantItem CreateItem(
        Guid id,
        string sku,
        Guid expectedConcurrencyToken,
        string value = "Standard",
        int stock = 0)
    {
        return new BulkUpdateProductVariantItem(
            id,
            "Uzunluk",
            value,
            sku,
            100m,
            stock,
            expectedConcurrencyToken,
            StockAdjustmentReason: "Batch stock count");
    }

    // Burada verilen varyantları takipli ve SKU çakışmasız döndüren repository mock'unu hazırlıyorum.
    private static Mock<IProductVariantRepository> CreateRepositoryMock(params ProductVariant[] variants)
    {
        var repository = new Mock<IProductVariantRepository>();
        repository
            .Setup(item => item.GetByIdsWithDetailsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants.OrderBy(variant => variant.Id).ToList());
        repository
            .Setup(item => item.GetExistingSkusAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return repository;
    }

    // Burada her seçenek isteğini geçerli tekli merkezi seçenek kaydıyla karşılayan resolver mock'unu hazırlıyorum.
    private static Mock<IVariantOptionResolver> CreateOptionResolverMock()
    {
        var resolver = new Mock<IVariantOptionResolver>();
        resolver
            .Setup(item => item.ResolveCompositeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string value, CancellationToken _) =>
            {
                var optionName = new VariantOptionName(name);
                var optionValue = new VariantOptionValue(optionName, value);
                return new[] { new VariantOptionSelection(optionName, optionValue) };
            });
        return resolver;
    }

    // Burada serializable callback'i gerçekten çalıştırıp SaveChanges çağrılarını sayan UnitOfWork mock'unu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateExecutingUnitOfWorkMock()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<ProductVariantDto>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<IReadOnlyList<ProductVariantDto>>> operation, CancellationToken token) =>
                operation(token));
        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }
}
