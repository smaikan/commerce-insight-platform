using ECommerce.Application.Brands.Commands.DeleteBrand;
using ECommerce.Application.Collections.Commands.DeleteCollection;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Products.Commands.DeleteProduct;
using ECommerce.Application.ProductTypes.Commands.DeleteProductType;
using ECommerce.Application.Tags.Commands.DeleteTag;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class CatalogDeletionCommandHandlerTests
{
    private static readonly DateTime DeletedAtUtc = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    // Burada ürün silmenin fiziksel silme yerine arşivleyen soft delete uyguladığını doğruluyorum.
    [Fact]
    public async Task DeleteProduct_Should_Soft_Delete_Product()
    {
        var repository = new Mock<IProductRepository>();
        var unitOfWork = CreateUnitOfWork();
        var product = new Product(
            "Product",
            "product",
            "PRODUCT-MAIN",
            status: ProductStatus.Active,
            isFeatured: true).WithId(11);
        repository.Setup(item => item.GetByIdForDeletionAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await new DeleteProductCommandHandler(repository.Object, unitOfWork.Object, new FixedClock(DeletedAtUtc))
            .Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        product.DeletedAtUtc.Should().Be(DeletedAtUtc);
        product.IsDeleted.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Archived);
        product.IsActive.Should().BeFalse();
        product.IsFeatured.Should().BeFalse();
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada daha önce silinen ürün için tekrar DELETE çağrısının idempotent olduğunu doğruluyorum.
    [Fact]
    public async Task DeleteProduct_Should_Be_Idempotent_When_Product_Is_Already_Deleted()
    {
        var repository = new Mock<IProductRepository>();
        var unitOfWork = CreateUnitOfWork();
        var product = new Product("Product", "product", "PRODUCT-MAIN").WithId(12);
        product.SoftDelete(DeletedAtUtc);
        repository.Setup(item => item.GetByIdForDeletionAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await new DeleteProductCommandHandler(repository.Object, unitOfWork.Object, new FixedClock(DeletedAtUtc.AddHours(1)))
            .Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        product.DeletedAtUtc.Should().Be(DeletedAtUtc);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada bulunmayan ürün silme isteğinin 404 sözleşmesine dönüşecek hatayı ürettiğini doğruluyorum.
    [Fact]
    public async Task DeleteProduct_Should_Throw_NotFound_When_Product_Does_Not_Exist()
    {
        var repository = new Mock<IProductRepository>();
        var unitOfWork = CreateUnitOfWork();
        repository.Setup(item => item.GetByIdForDeletionAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        Func<Task> act = () => new DeleteProductCommandHandler(repository.Object, unitOfWork.Object, new FixedClock(DeletedAtUtc))
            .Handle(new DeleteProductCommand(99), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada marka silmenin ürün kullanım kontrolüne bağlı olmadan kaydı kaldırdığını doğruluyorum.
    [Fact]
    public async Task DeleteBrand_Should_Remove_Brand_Without_Usage_Block()
    {
        var repository = new Mock<IBrandRepository>();
        var unitOfWork = CreateUnitOfWork();
        var brand = new Brand("Brand", "brand");
        repository.Setup(item => item.GetByIdForUpdateAsync(brand.Id, It.IsAny<CancellationToken>())).ReturnsAsync(brand);

        await new DeleteBrandCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new DeleteBrandCommand(brand.Id), CancellationToken.None);

        repository.Verify(item => item.Remove(brand), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada koleksiyon silmenin ürün kullanım kontrolüne bağlı olmadan kaydı kaldırdığını doğruluyorum.
    [Fact]
    public async Task DeleteCollection_Should_Remove_Collection_Without_Usage_Block()
    {
        var repository = new Mock<ICollectionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var collection = new Collection("Collection", "collection");
        repository.Setup(item => item.GetByIdForUpdateAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collection);

        await new DeleteCollectionCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new DeleteCollectionCommand(collection.Id), CancellationToken.None);

        repository.Verify(item => item.Remove(collection), Times.Once);
    }

    // Burada ürün türü silmenin ürün kullanım kontrolüne bağlı olmadan kaydı kaldırdığını doğruluyorum.
    [Fact]
    public async Task DeleteProductType_Should_Remove_Type_Without_Usage_Block()
    {
        var repository = new Mock<IProductTypeRepository>();
        var unitOfWork = CreateUnitOfWork();
        var productType = new ProductType("Type");
        repository.Setup(item => item.GetByIdForUpdateAsync(productType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(productType);

        await new DeleteProductTypeCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new DeleteProductTypeCommand(productType.Id), CancellationToken.None);

        repository.Verify(item => item.Remove(productType), Times.Once);
    }

    // Burada etiket silmenin ürün kullanım kontrolüne bağlı olmadan kaydı kaldırdığını doğruluyorum.
    [Fact]
    public async Task DeleteTag_Should_Remove_Tag_Without_Usage_Block()
    {
        var repository = new Mock<ITagRepository>();
        var unitOfWork = CreateUnitOfWork();
        var tag = new Tag("Tag", "tag");
        repository.Setup(item => item.GetByIdForUpdateAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        await new DeleteTagCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new DeleteTagCommand(tag.Id), CancellationToken.None);

        repository.Verify(item => item.Remove(tag), Times.Once);
    }

    // Burada başarılı kayıt sonucunu ortak biçimde hazırlayan transaction mock'unu oluşturuyorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; }

        // Burada ürün silme testleri için sabit UTC zamanı hazırlıyorum.
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }
    }
}
