using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Queries.GetPublishedProducts;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class PublishedProductsQueryHandlerTests
{
    // Burada varsayılan storefront sıralamasının en yeni ürünler önce olduğunu doğruluyorum.
    [Fact]
    public async Task Handle_Should_Use_Newest_Descending_By_Default()
    {
        var reader = new Mock<IPublishedProductListReader>();
        reader
            .Setup(item => item.GetListAsync(It.IsAny<PublishedProductListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublishedProductListItemDto>([], 1, 24, 0));
        var handler = new GetPublishedProductsQueryHandler(reader.Object);

        await handler.Handle(new GetPublishedProductsQuery(), CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<PublishedProductListFilter>(filter =>
                filter.SortBy == PublishedProductSortBy.Newest && filter.Descending),
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
        var handler = new GetPublishedProductsQueryHandler(reader.Object);

        await handler.Handle(
            new GetPublishedProductsQuery(SortBy: PublishedProductSortBy.Popularity, Descending: true),
            CancellationToken.None);

        reader.Verify(item => item.GetListAsync(
            It.Is<PublishedProductListFilter>(filter =>
                filter.SortBy == PublishedProductSortBy.Popularity && filter.Descending),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
