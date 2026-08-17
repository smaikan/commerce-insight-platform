using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Application.ProductTypes.Queries.GetPublishedProductTypeShowcase;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class PublishedProductTypeShowcaseQueryHandlerTests
{
    // Burada storefront görünürlük ayarlarının kategori vitrini okuyucusuna tek kez aktarıldığını doğruluyorum.
    [Fact]
    public async Task Handle_Should_Apply_Current_Storefront_Visibility_Settings()
    {
        var settings = StoreSettings.CreateDefault();
        settings.UpdateStorefront(
            ECommerce.Domain.Enums.StorefrontStatus.Active,
            null,
            showOutOfStockProducts: false,
            showProductsWithoutPrice: false,
            ECommerce.Domain.Enums.StorefrontProductSort.Newest,
            defaultProductSortDescending: true,
            showCompareAtPrice: true,
            showStockWarning: true,
            lowStockThreshold: 5);
        var reader = new Mock<IPublishedProductTypeShowcaseReader>();
        reader.Setup(service => service.GetListAsync(
                It.IsAny<PublishedProductTypeShowcaseFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedProductTypeShowcaseItemDto>([], 2, 12, 0));
        var settingsRepository = new Mock<IStoreSettingsRepository>();
        settingsRepository.Setup(repository => repository.GetAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        var handler = new GetPublishedProductTypeShowcaseQueryHandler(
            reader.Object,
            settingsRepository.Object);

        var result = await handler.Handle(
            new GetPublishedProductTypeShowcaseQuery(2, 12),
            CancellationToken.None);

        result.PageNumber.Should().Be(2);
        reader.Verify(service => service.GetListAsync(
            It.Is<PublishedProductTypeShowcaseFilter>(filter =>
                filter.PageNumber == 2 &&
                filter.PageSize == 12 &&
                !filter.ShowOutOfStockProducts &&
                !filter.ShowProductsWithoutPrice),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada kategori vitrini validator'ının geçersiz sayfa değerlerini reddettiğini doğruluyorum.
    [Fact]
    public void Validator_Should_Reject_Invalid_Paging()
    {
        var validator = new GetPublishedProductTypeShowcaseQueryValidator();

        var result = validator.Validate(new GetPublishedProductTypeShowcaseQuery(0, 101));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName)
            .Should().BeEquivalentTo(["PageNumber", "PageSize"]);
    }
}
