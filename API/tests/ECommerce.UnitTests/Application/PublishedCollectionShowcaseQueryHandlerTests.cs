using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Collections.Queries.GetPublishedCollectionShowcase;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class PublishedCollectionShowcaseQueryHandlerTests
{
    // Burada güncel storefront görünürlük ayarlarının koleksiyon vitrin okuyucusuna tek kez aktarıldığını doğruluyorum.
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
        var reader = new Mock<IPublishedCollectionShowcaseReader>();
        reader.Setup(service => service.GetListAsync(
                It.IsAny<PublishedCollectionShowcaseFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedCollectionShowcaseItemDto>([], 2, 12, 0));
        var settingsRepository = new Mock<IStoreSettingsRepository>();
        settingsRepository.Setup(repository => repository.GetAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        var handler = new GetPublishedCollectionShowcaseQueryHandler(
            reader.Object,
            settingsRepository.Object);

        var result = await handler.Handle(
            new GetPublishedCollectionShowcaseQuery(2, 12),
            CancellationToken.None);

        result.PageNumber.Should().Be(2);
        reader.Verify(service => service.GetListAsync(
            It.Is<PublishedCollectionShowcaseFilter>(filter =>
                filter.PageNumber == 2 &&
                filter.PageSize == 12 &&
                !filter.ShowOutOfStockProducts &&
                !filter.ShowProductsWithoutPrice),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
