using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.Services;

// Burada kesin ödeme ve ters işlem kaynaklı satış metriği değişikliklerinin Application sözleşmesini tanımlıyorum.
public interface IAuthoritativeSalesMetricService
{
    // Burada Paid siparişi idempotent biçimde satış metriğine ekleme sözleşmesini tanımlıyorum.
    Task RecordPaidOrderAsync(Order order, CancellationToken cancellationToken = default);

    // Burada finansal iptali idempotent biçimde satış metriğinden düşme sözleşmesini tanımlıyorum.
    Task ReverseCancelledOrderAsync(Order order, CancellationToken cancellationToken = default);

    // Burada onaylı refund kalemlerini idempotent biçimde satış metriğinden düşme sözleşmesini tanımlıyorum.
    Task ReverseApprovedRefundAsync(
        Order order,
        ReturnRequest returnRequest,
        CancellationToken cancellationToken = default);
}

// Burada yalnız kesinleşmiş ödeme ve finansal ters işlemleri ürün bazlı net satış metriğine yansıtıyorum.
public sealed class AuthoritativeSalesMetricService : IAuthoritativeSalesMetricService
{
    private readonly IProductRepository _products;

    // Burada satış metriği servisini ürün aggregate deposuyla hazırlıyorum.
    public AuthoritativeSalesMetricService(IProductRepository products)
    {
        _products = products;
    }

    // Burada Paid olan siparişin her kalemini yeniden denemelerde çift saymadan satış metriğine ekliyorum.
    public Task RecordPaidOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Status is not OrderStatus.Paid and not OrderStatus.Preparing and not OrderStatus.Shipped and
            not OrderStatus.Delivered and not OrderStatus.ReturnRequested and not OrderStatus.ReturnApproved and
            not OrderStatus.Refunded)
        {
            throw new ConflictException("Only a paid order can be recorded as an authoritative sale.");
        }

        return ApplyAsync(
            order.Items.Select(item => new SalesMetricChange(item, item.RecordPaidSale(), false)),
            cancellationToken);
    }

    // Burada tamamlanan sipariş iptalinin bütün kalemlerini yeniden denemelerde çift azaltmadan tersliyorum.
    public Task ReverseCancelledOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        return ApplyAsync(
            order.Items.Select(item => new SalesMetricChange(item, item.ReversePaidSale(item.Quantity), true)),
            cancellationToken);
    }

    // Burada onaylanan refund talebinin yalnız ilgili kalem ve adetlerini net satıştan düşürüyorum.
    public Task ReverseApprovedRefundAsync(
        Order order,
        ReturnRequest returnRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(returnRequest);
        if (returnRequest.Type != ReturnType.Refund || !returnRequest.IsCompletedRefund())
        {
            return Task.CompletedTask;
        }

        var orderItems = order.Items.ToDictionary(item => item.Id);
        var changes = returnRequest.Items.Select(returnItem =>
        {
            if (!orderItems.TryGetValue(returnItem.OrderItemId, out var orderItem))
            {
                throw new ConflictException("Return item does not belong to the loaded order.");
            }

            var quantityToReverse = returnItem.GetPendingSalesMetricReversalQuantity();
            if (quantityToReverse <= 0)
            {
                return new SalesMetricChange(orderItem, 0, true);
            }

            var reversedQuantity = orderItem.ReversePaidSale(quantityToReverse);
            if (reversedQuantity > 0)
            {
                returnItem.RecordSalesMetricReversal(reversedQuantity);
            }

            return new SalesMetricChange(orderItem, reversedQuantity, true);
        });
        return ApplyAsync(changes, cancellationToken);
    }

    // Burada kalem bazlı değişimleri ürün başına birleştirip takipli ürün aggregate'larına uygularım.
    private async Task ApplyAsync(
        IEnumerable<SalesMetricChange> changes,
        CancellationToken cancellationToken)
    {
        var materializedChanges = changes.Where(change => change.Quantity > 0).ToList();
        if (materializedChanges.Count == 0)
        {
            return;
        }

        var deltas = materializedChanges
            .GroupBy(change => change.OrderItem.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(change => change.IsReversal ? -(long)change.Quantity : change.Quantity));
        var products = await _products.GetByIdsForSalesMetricUpdateAsync(deltas.Keys, cancellationToken);
        if (products.Count != deltas.Count)
        {
            throw new ConflictException("A sales metric product could not be found.");
        }

        foreach (var product in products)
        {
            var delta = deltas[product.Id];
            if (delta > 0)
            {
                product.IncreaseNetSalesQuantity(delta);
            }
            else if (delta < 0)
            {
                product.DecreaseNetSalesQuantity(-delta);
            }
        }
    }

    private sealed record SalesMetricChange(OrderItem OrderItem, int Quantity, bool IsReversal);
}
