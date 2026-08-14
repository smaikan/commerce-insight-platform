using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Queries.GetPublishedProducts;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class PublishedProductsQueryHandlerTests
{
    // Burada arama kullanılıp explicit sort verilmediğinde relevance sıralamasının seçildiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Use_Relevance_When_Search_Has_No_Explicit_Sort()
    {
        var reader = new Mock<IPublishedProductListReader>();
        reader.Setup(item => item.GetListAsync(It.IsAny<PublishedProductListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedProductListItemDto>([], 1, 24, 0));
        var settingsRepository = new Mock<IStoreSettingsRepository>();
        settingsRepository.Setup(repository => repository.GetAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ECommerce.Domain.Entities.StoreSettings.CreateDefault());
        var handler = new GetPublishedProductsQueryHandler(reader.Object);

        await handler.Handle(new GetPublishedProductsQuery(Search: "  ŞÖNİL   kolye  "), CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<PublishedProductListFilter>(filter =>
                filter.SortBy == null &&
                filter.SearchNormalized == "sonil kolye" &&
                filter.SearchTokens!.SequenceEqual(new[] { "sonil", "kolye" }) &&
                filter.CandidateGrams!.SequenceEqual(new[] { "son", "kol" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada explicit katalog sıralamasının arama relevance varsayılanını ezdiğini doğruluyorum.
    [Fact]
    public async Task Handle_Should_Preserve_Explicit_Sort_When_Search_Is_Used()
    {
        var reader = new Mock<IPublishedProductListReader>();
        reader.Setup(item => item.GetListAsync(It.IsAny<PublishedProductListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedProductListItemDto>([], 1, 24, 0));
        var settingsRepository = new Mock<IStoreSettingsRepository>();
        settingsRepository.Setup(repository => repository.GetAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ECommerce.Domain.Entities.StoreSettings.CreateDefault());
        var handler = new GetPublishedProductsQueryHandler(reader.Object);

        await handler.Handle(new GetPublishedProductsQuery(
            SortBy: PublishedProductSortBy.Title,
            Descending: false,
            Search: "kolye"), CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<PublishedProductListFilter>(filter =>
                filter.SortBy == PublishedProductSortBy.Title && filter.Descending == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada varsayılan storefront sıralamasının en yeni ürünler önce olduğunu doğruluyorum.
    [Fact]
    public async Task Handle_Should_Use_Newest_Descending_By_Default()
    {
        var reader = new Mock<IPublishedProductListReader>();
        reader
            .Setup(item => item.GetListAsync(It.IsAny<PublishedProductListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedProductListItemDto>([], 1, 24, 0));
        var settingsRepository = new Mock<IStoreSettingsRepository>();
        settingsRepository.Setup(repository => repository.GetAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ECommerce.Domain.Entities.StoreSettings.CreateDefault());
        var handler = new GetPublishedProductsQueryHandler(reader.Object);

        await handler.Handle(new GetPublishedProductsQuery(), CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<PublishedProductListFilter>(filter =>
                filter.SortBy == null && filter.Descending == null && filter.ResolveStoreSettings),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada storefront tarafından seçilen sıralama seçeneğinin veri okuyucuya aynen aktarıldığını doğruluyorum.
    [Fact]
    public async Task Handle_Should_Preserve_Selected_Sort()
    {
        var reader = new Mock<IPublishedProductListReader>();
        reader
            .Setup(item => item.GetListAsync(It.IsAny<PublishedProductListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedProductListItemDto>([], 1, 24, 0));
        var settingsRepository = new Mock<IStoreSettingsRepository>();
        settingsRepository.Setup(repository => repository.GetAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ECommerce.Domain.Entities.StoreSettings.CreateDefault());
        var handler = new GetPublishedProductsQueryHandler(reader.Object);

        await handler.Handle(
            new GetPublishedProductsQuery(SortBy: PublishedProductSortBy.Popularity, Descending: true),
            CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<PublishedProductListFilter>(filter =>
                filter.SortBy == PublishedProductSortBy.Popularity && filter.Descending == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
