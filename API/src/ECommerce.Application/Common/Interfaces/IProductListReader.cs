using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductListReader
{
    // Burada katalog listelemesinin yalnız API sözleşmesinde gereken verilerle okunmasını tanımlıyorum.
    Task<PagedResult<ProductDto>> GetListAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default);
}
