using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Queries.GetProducts;
using ECommerce.Application.Products.Queries.GetPublishedProducts;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductListFilteringQueryHandlerTests
{
    // Burada admin sınıflandırma filtrelerinin read-model filtresine eksiksiz aktarıldığını doğruluyorum.
    [Fact]
    public async Task Admin_Handler_Should_Forward_All_Taxonomy_Filters()
    {
        var typeId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var reader = new Mock<IProductListReader>();
        reader.Setup(item => item.GetListAsync(It.IsAny<ProductListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductDto>([], 1, 20, 0));
        var handler = new GetProductsQueryHandler(reader.Object);

        await handler.Handle(new GetProductsQuery(
            TypeId: typeId,
            BrandId: brandId,
            CollectionId: collectionId,
            TagId: tagId), CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<ProductListFilter>(filter =>
                filter.TypeId == typeId &&
                filter.BrandId == brandId &&
                filter.CollectionId == collectionId &&
                filter.TagId == tagId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada storefront sınıflandırma filtrelerinin yayın listesi filtresine eksiksiz aktarıldığını doğruluyorum.
    [Fact]
    public async Task Storefront_Handler_Should_Forward_All_Taxonomy_Filters()
    {
        var typeId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var reader = new Mock<IPublishedProductListReader>();
        reader.Setup(item => item.GetListAsync(It.IsAny<PublishedProductListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedProductListItemDto>([], 1, 24, 0));
        var settingsRepository = new Mock<IStoreSettingsRepository>();
        settingsRepository.Setup(repository => repository.GetAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ECommerce.Domain.Entities.StoreSettings.CreateDefault());
        var handler = new GetPublishedProductsQueryHandler(reader.Object);

        await handler.Handle(new GetPublishedProductsQuery(
            TypeId: typeId,
            BrandId: brandId,
            CollectionId: collectionId,
            TagId: tagId), CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<PublishedProductListFilter>(filter =>
                filter.TypeId == typeId &&
                filter.BrandId == brandId &&
                filter.CollectionId == collectionId &&
                filter.TagId == tagId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada admin filtrelerinde boş GUID değerlerinin validation hatası verdiğini doğruluyorum.
    [Fact]
    public void Admin_Validator_Should_Reject_Empty_Taxonomy_Ids()
    {
        var result = new GetProductsQueryValidator().TestValidate(new GetProductsQuery(
            TypeId: Guid.Empty,
            BrandId: Guid.Empty,
            CollectionId: Guid.Empty,
            TagId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(query => query.TypeId);
        result.ShouldHaveValidationErrorFor(query => query.BrandId);
        result.ShouldHaveValidationErrorFor(query => query.CollectionId);
        result.ShouldHaveValidationErrorFor(query => query.TagId);
    }

    // Burada storefront filtrelerinde boş GUID değerlerinin validation hatası verdiğini doğruluyorum.
    [Fact]
    public void Storefront_Validator_Should_Reject_Empty_Taxonomy_Ids()
    {
        var result = new GetPublishedProductsQueryValidator().TestValidate(new GetPublishedProductsQuery(
            TypeId: Guid.Empty,
            BrandId: Guid.Empty,
            CollectionId: Guid.Empty,
            TagId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(query => query.TypeId);
        result.ShouldHaveValidationErrorFor(query => query.BrandId);
        result.ShouldHaveValidationErrorFor(query => query.CollectionId);
        result.ShouldHaveValidationErrorFor(query => query.TagId);
    }
}
