using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IPublishedProductSearchReader
{
    // Burada public ürün önerilerini tek SQL komutuyla okuyan sözleşmeyi tanımlıyorum.
    Task<PublishedProductSearchSuggestionsDto> GetSuggestionsAsync(
        PublishedProductSearchFilter filter,
        CancellationToken cancellationToken = default);
}
