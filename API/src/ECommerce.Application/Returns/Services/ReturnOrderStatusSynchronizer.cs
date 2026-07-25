using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Returns.Services;

public static class ReturnOrderStatusSynchronizer
{
    // Burada aynı siparişin tüm iade taleplerinden tek ve anlamlı sipariş durumunu üretiyorum.
    public static void Synchronize(Order order, IReadOnlyCollection<ReturnRequest> returnRequests)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(returnRequests);

        if (returnRequests.Any(request => request.Status is
                ReturnRequestStatus.Approved or
                ReturnRequestStatus.Received or
                ReturnRequestStatus.Completed))
        {
            order.MarkReturnApproved();
            return;
        }

        if (returnRequests.Any(request => request.Status == ReturnRequestStatus.Requested))
        {
            order.MarkReturnRequested();
            return;
        }

        order.RestoreDeliveredAfterReturnResolution();
    }
}
