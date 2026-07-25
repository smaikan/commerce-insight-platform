using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly AppDbContext _context;

    // Burada iade talebi sorgu ve değişiklikleri için istek kapsamındaki DbContext'i hazırlıyorum.
    public ReturnRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni iade talebi aggregate'ını kalemleriyle birlikte veritabanı takibine ekliyorum.
    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
    {
        await _context.Set<ReturnRequest>().AddAsync(returnRequest, cancellationToken);
    }

    // Burada iade talebini yalnız gerçek sahibi için detay grafiğiyle takip etmeden getiriyorum.
    public Task<ReturnRequest?> GetByIdForUserAsync(
        Guid returnRequestId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return CreateReadGraphQuery()
            .FirstOrDefaultAsync(request => request.Id == returnRequestId && request.UserId == userId, cancellationToken);
    }

    // Burada iade talebini yönetim detay ekranı için takip etmeden getiriyorum.
    public Task<ReturnRequest?> GetByIdAsync(Guid returnRequestId, CancellationToken cancellationToken = default)
    {
        return CreateReadGraphQuery()
            .FirstOrDefaultAsync(request => request.Id == returnRequestId, cancellationToken);
    }

    // Burada iade talebini yalnız gerçek sahibi için değişiklik akışında takipli getiriyorum.
    public Task<ReturnRequest?> GetByIdForUserForUpdateAsync(
        Guid returnRequestId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return CreateUpdateGraphQuery()
            .FirstOrDefaultAsync(request => request.Id == returnRequestId && request.UserId == userId, cancellationToken);
    }

    // Burada iade talebini yönetim iş akışında takipli getiriyorum.
    public Task<ReturnRequest?> GetByIdForUpdateAsync(Guid returnRequestId, CancellationToken cancellationToken = default)
    {
        return CreateUpdateGraphQuery()
            .FirstOrDefaultAsync(request => request.Id == returnRequestId, cancellationToken);
    }

    // Burada aynı sipariş için daha önce ayrılmış iade adetlerini işlem kilidi altında getiriyorum.
    public async Task<IReadOnlyList<ReturnRequest>> GetByOrderIdForUpdateAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await CreateUpdateGraphQuery()
            .Where(request => request.OrderId == orderId)
            .OrderBy(request => request.CreatedAt)
            .ThenBy(request => request.Id)
            .ToListAsync(cancellationToken);
    }

    // Burada kullanıcının kendi iade taleplerini zorunlu owner filtresiyle sayfalıyorum.
    public Task<PagedResult<ReturnRequest>> GetListForUserAsync(
        ReturnRequestListFilter filter,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return GetListInternalAsync(filter with { UserId = userId }, cancellationToken);
    }

    // Burada yönetim iade taleplerini istenen filtrelerle sayfalıyorum.
    public Task<PagedResult<ReturnRequest>> GetListAsync(
        ReturnRequestListFilter filter,
        CancellationToken cancellationToken = default)
    {
        return GetListInternalAsync(filter, cancellationToken);
    }

    // Burada iade liste sorgusunu filtre, kararlı sıralama ve sınırlı sayfa boyutuyla yürütüyorum.
    private async Task<PagedResult<ReturnRequest>> GetListInternalAsync(
        ReturnRequestListFilter filter,
        CancellationToken cancellationToken)
    {
        IQueryable<ReturnRequest> query = _context.Set<ReturnRequest>().AsNoTracking();
        if (filter.UserId.HasValue)
        {
            query = query.Where(request => request.UserId == filter.UserId.Value);
        }

        if (filter.OrderId.HasValue)
        {
            query = query.Where(request => request.OrderId == filter.OrderId.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(request => request.Type == filter.Type.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(request => request.Status == filter.Status.Value);
        }

        if (filter.CreatedFromUtc.HasValue)
        {
            query = query.Where(request => request.CreatedAt >= filter.CreatedFromUtc.Value);
        }

        if (filter.CreatedToUtc.HasValue)
        {
            query = query.Where(request => request.CreatedAt <= filter.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((filter.PageNumber - 1) * filter.PageSize);
        var items = await query
            .Include(request => request.Order)
            .Include(request => request.Items)
            .AsSplitQuery()
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<ReturnRequest>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    // Burada okuma ekranları için iade talebi, sipariş ve kalem grafiğini hazırlıyorum.
    private IQueryable<ReturnRequest> CreateReadGraphQuery()
    {
        return CreateUpdateGraphQuery().AsNoTracking();
    }

    // Burada iş akışı güncellemeleri için iade talebinin gerekli takipli grafiğini hazırlıyorum.
    private IQueryable<ReturnRequest> CreateUpdateGraphQuery()
    {
        return _context.Set<ReturnRequest>()
            .Include(request => request.Order)
            .Include(request => request.Items)
            .AsSplitQuery();
    }
}
