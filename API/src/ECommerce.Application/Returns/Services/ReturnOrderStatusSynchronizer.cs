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

        if (returnRequests.Any(request =>
                request.Type == ReturnType.Refund &&
                request.RepresentsApprovedOutcome()))
        {
            order.MarkRefunded();
            return;
        }

        if (returnRequests.Any(request =>
                request.Type == ReturnType.Exchange &&
                request.RepresentsApprovedOutcome()))
        {
            order.MarkReturnApproved();
            return;
        }

        if (returnRequests.Any(request =>
                request.Status == ReturnRequestStatus.Requested || request.IsAwaitingDecision()))
        {
            order.MarkReturnRequested();
            return;
        }

        order.RestoreDeliveredAfterReturnResolution();
    }
}
