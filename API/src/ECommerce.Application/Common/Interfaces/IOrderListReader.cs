using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;

namespace ECommerce.Application.Common.Interfaces;

// Burada sipariş listelerini aggregate grafiği yüklemeden özet modele okuyacak sınırı tanımlıyorum.
public interface IOrderListReader
{
    Task<PagedResult<OrderSummaryDto>> GetListAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken = default);
}
