using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

// Burada sipariş özet ekranları için yalnızca gereken kolonları doğrudan okuyorum.
public sealed class OrderListReader : IOrderListReader
{
    private readonly AppDbContext _context;

    // Burada liste sorgusu için istek kapsamındaki DbContext'i hazırlıyorum.
    public OrderListReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada sayfa verisini kalem grafiğini materialize etmeden özet DTO'ya projekte ediyorum.
    public async Task<PagedResult<OrderSummaryDto>> GetListAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.Orders.AsNoTracking(), filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((filter.PageNumber - 1) * filter.PageSize);
        var items = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(order => new OrderSummaryDto(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.GrandTotal,
                order.Items.Count,
                order.CreatedAt,
                order.PaidAt,
                order.CustomerSnapshot == null
                    ? null
                    : order.CustomerSnapshot.FirstName + " " + order.CustomerSnapshot.LastName))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryDto>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    // Burada yönetici ve sahip kapsamındaki ortak filtreleri sayım ve veri sorgusuna birlikte uyguluyorum.
    private static IQueryable<Order> ApplyFilter(IQueryable<Order> query, OrderListFilter filter)
    {
        if (filter.UserId.HasValue) query = query.Where(order => order.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(order =>
                order.OrderNumber.Contains(search) ||
                (order.CustomerSnapshot != null && (
                    order.CustomerSnapshot.FirstName.Contains(search) ||
                    order.CustomerSnapshot.LastName.Contains(search) ||
                    order.CustomerSnapshot.Email.Contains(search))));
        }
        if (filter.Status.HasValue) query = query.Where(order => order.Status == filter.Status.Value);
        if (filter.CreatedFromUtc.HasValue) query = query.Where(order => order.CreatedAt >= filter.CreatedFromUtc.Value);
        if (filter.CreatedToUtc.HasValue) query = query.Where(order => order.CreatedAt <= filter.CreatedToUtc.Value);
        return query;
    }
}
