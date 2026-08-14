using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class StoreSettingsRepository : IStoreSettingsRepository
{
    private readonly AppDbContext _context;

    // Burada tek mağaza ayarı sorgusu için istek kapsamındaki DbContext'i hazırlıyorum.
    public StoreSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada sabit anahtarlı mağaza ayarını izleme tercihine göre tek sorguyla okuyorum.
    public Task<StoreSettings?> GetAsync(
        bool asTracking,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StoreSettings> query = _context.StoreSettings;
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(
            settings => settings.Id == StoreSettings.SingletonId,
            cancellationToken);
    }

    // Burada eksik singleton ayarını aynı DbContext'e ekliyorum.
    public void Add(StoreSettings settings) =>
        _context.StoreSettings.Add(settings);
}
